using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Finance.GetScenarios;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Finance.GetScenarios;

public sealed class GetScenariosQueryHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IOperatingCostRepository _costRepository = Substitute.For<IOperatingCostRepository>();
    private readonly IStockMovementRepository _stockMovementRepository = Substitute.For<IStockMovementRepository>();
    private readonly IStockItemRepository _stockItemRepository = Substitute.For<IStockItemRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetScenariosQueryHandler _handler;

    public GetScenariosQueryHandlerTests()
    {
        _handler = new GetScenariosQueryHandler(
            _saleRepository, _costRepository, _stockMovementRepository, _stockItemRepository,
            _productRepository, _logRepository, _unitOfWork);

        // Defaults neutros — cada teste sobrescreve apenas o que precisar.
        _saleRepository.GetByBranchAndPeriodAsync(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Sale>());
        _costRepository.GetByBranchAndMonthAsync(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OperatingCost>());
        _stockMovementRepository.GetSaleCostAsync(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(0m);
        _stockMovementRepository.GetSaleQuantitiesByProductAsync(Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProductQuantity>());
        _stockItemRepository.GetByBranchAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StockItem>());
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Product>());
    }

    // ---------- Helpers ----------

    // Mesmo contorno usado em GetSalesReportQueryHandlerTests: Product.Id só existe após
    // persistência real (fábrica sempre cria com Id=0), mas o handler casa
    // ProductQuantity.ProductId com Product.Id dentro da coleção devolvida por GetByIdsAsync.
    // Usamos reflection para simular o Id que o EF Core atribuiria.
    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static Product CreateProduct(long id, string name, decimal salePrice)
    {
        var product = Product.Create(
            companyId: 1, categoryId: 1, unitOfMeasureId: 1, name: name, description: null,
            barcode: null, salePrice: salePrice, costPrice: null, isStockControlled: true,
            preparationTimeMinutes: null).Value;
        SetId(product, id);
        return product;
    }

    private static Sale CreateSale(decimal subtotal, long saleNumber = 1)
        => Sale.Create(
            branchId: 1, customerOrderId: saleNumber, cashSessionId: 1, employeeId: 1,
            saleNumber: saleNumber, subtotalAmount: subtotal, discountAmount: 0m, serviceFeeAmount: 0m).Value;

    // ---------- Tests: validação ----------

    [Fact]
    public async Task Handle_InvalidReferenceMonth_ReturnsFailureWithoutCallingAnyRepository()
    {
        var query = new GetScenariosQuery(
            BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 13, DesiredProfit: 1000m,
            PessimisticMargin: null, NormalMargin: null, OptimisticMargin: null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scenarios.InvalidMonth");
        await _saleRepository.DidNotReceive().GetByBranchAndPeriodAsync(
            Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _costRepository.DidNotReceive().GetByBranchAndMonthAsync(
            Arg.Any<long>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NegativeDesiredProfit_ReturnsFailureWithoutCallingAnyRepository()
    {
        var query = new GetScenariosQuery(
            BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 8, DesiredProfit: -1m,
            PessimisticMargin: null, NormalMargin: null, OptimisticMargin: null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scenarios.InvalidProfit");
        await _stockMovementRepository.DidNotReceive().GetSaleCostAsync(
            Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    // ---------- Tests: margem histórica x margem informada ----------

    [Fact]
    public async Task Handle_HistoricalRevenuePositiveAndNoMarginProvided_UsesHistoricalMarginAsNormal()
    {
        var query = new GetScenariosQuery(
            BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 8, DesiredProfit: 500m,
            PessimisticMargin: null, NormalMargin: null, OptimisticMargin: null);

        var sale = CreateSale(subtotal: 1000m);
        _saleRepository.GetByBranchAndPeriodAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { sale });
        // CMV=300, custos variáveis=100 -> margem histórica = 1 - (300+100)/1000 = 0.6
        _stockMovementRepository.GetSaleCostAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(300m);
        var variableCost = OperatingCost.Create(1, CostTypeIds.Variavel, "Comissões", 100m, 2026, 8).Value;
        var fixedCost = OperatingCost.Create(1, CostTypeIds.Fixo, "Aluguel", 2000m, 2026, 8).Value;
        _costRepository.GetByBranchAndMonthAsync(1, 2026, 8, Arg.Any<CancellationToken>())
            .Returns(new[] { variableCost, fixedCost });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.HistoricalMarginRate.Should().Be(0.6m);
        response.HistoricalRevenue.Should().Be(1000m);
        response.AverageTicket.Should().Be(1000m);
        response.FixedCosts.Should().Be(2000m);

        var normal = response.Scenarios.Single(s => s.Name == "Normal");
        normal.MarginRate.Should().Be(0.6m);
        normal.BreakEvenRevenue.Should().Be(Math.Round(2000m / 0.6m, 2));
        normal.TargetRevenue.Should().Be(Math.Round((2000m + 500m) / 0.6m, 2));

        // Pessimista/otimista derivam de 80%/120% da margem normal quando não informadas.
        var pessimistic = response.Scenarios.Single(s => s.Name == "Pessimista");
        var optimistic = response.Scenarios.Single(s => s.Name == "Otimista");
        pessimistic.MarginRate.Should().Be(Math.Round(0.6m * 0.8m, 4));
        optimistic.MarginRate.Should().Be(Math.Round(0.6m * 1.2m, 4));
    }

    [Fact]
    public async Task Handle_ExplicitMarginsProvided_UsesProvidedValuesInsteadOfHistorical()
    {
        var query = new GetScenariosQuery(
            BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 8, DesiredProfit: 0m,
            PessimisticMargin: 0.20m, NormalMargin: 0.50m, OptimisticMargin: 0.80m);

        var sale = CreateSale(subtotal: 1000m);
        _saleRepository.GetByBranchAndPeriodAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { sale });
        // Margem histórica seria 0.90 (CMV baixo) — bem diferente das margens explícitas abaixo.
        _stockMovementRepository.GetSaleCostAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(100m);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var scenarios = result.Value.Scenarios.ToDictionary(s => s.Name);
        scenarios["Pessimista"].MarginRate.Should().Be(0.20m);
        scenarios["Normal"].MarginRate.Should().Be(0.50m);
        scenarios["Otimista"].MarginRate.Should().Be(0.80m);
    }

    [Fact]
    public async Task Handle_MarginsOutOfAllowedRange_AreClampedBetween5And95Percent()
    {
        var query = new GetScenariosQuery(
            BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 8, DesiredProfit: 0m,
            PessimisticMargin: -0.5m, NormalMargin: 1.5m, OptimisticMargin: 2m);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var scenarios = result.Value.Scenarios.ToDictionary(s => s.Name);
        scenarios["Pessimista"].MarginRate.Should().Be(0.05m);
        scenarios["Normal"].MarginRate.Should().Be(0.95m);
        scenarios["Otimista"].MarginRate.Should().Be(0.95m);
    }

    [Fact]
    public async Task Handle_NoSalesInPeriod_HistoricalMarginIsNullAndDefaultMarginIsUsed()
    {
        var query = new GetScenariosQuery(
            BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 8, DesiredProfit: 0m,
            PessimisticMargin: null, NormalMargin: null, OptimisticMargin: null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.HistoricalRevenue.Should().BeNull();
        response.HistoricalMarginRate.Should().BeNull();
        response.AverageTicket.Should().BeNull();
        response.Scenarios.Single(s => s.Name == "Normal").MarginRate.Should().Be(0.30m);
    }

    // ---------- Tests: 3 cenários e fórmulas de breakeven/target ----------

    [Fact]
    public async Task Handle_Always_ReturnsExactlyThreeScenariosInPessimistaNormalOtimistaOrderWithConsistentFormulas()
    {
        var query = new GetScenariosQuery(
            BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 8, DesiredProfit: 500m,
            PessimisticMargin: 0.2m, NormalMargin: 0.4m, OptimisticMargin: 0.6m);

        var fixedCost = OperatingCost.Create(1, CostTypeIds.Fixo, "Aluguel", 1000m, 2026, 8).Value;
        _costRepository.GetByBranchAndMonthAsync(1, 2026, 8, Arg.Any<CancellationToken>())
            .Returns(new[] { fixedCost });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var scenarios = result.Value.Scenarios.ToList();
        scenarios.Should().HaveCount(3);
        scenarios.Select(s => s.Name).Should().ContainInOrder("Pessimista", "Normal", "Otimista");

        var daysInMonth = DateTime.DaysInMonth(2026, 8);
        foreach (var scenario in scenarios)
        {
            scenario.BreakEvenRevenue.Should().Be(Math.Round(1000m / scenario.MarginRate, 2));
            scenario.TargetRevenue.Should().Be(Math.Round((1000m + 500m) / scenario.MarginRate, 2));
            scenario.DailyTarget.Should().Be(Math.Round(scenario.TargetRevenue / daysInMonth, 2));
        }
    }

    // ---------- Tests: plano de estoque (BuildStockPlan) ----------

    [Fact]
    public async Task Handle_SoldQuantitiesWithMatchingProducts_BuildsNonEmptyStockPlanForEveryScenario()
    {
        var query = new GetScenariosQuery(
            BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 8, DesiredProfit: 1000m,
            PessimisticMargin: 0.3m, NormalMargin: 0.3m, OptimisticMargin: 0.3m);

        var product = CreateProduct(701, "Cerveja", salePrice: 10m);
        _stockMovementRepository.GetSaleQuantitiesByProductAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new ProductQuantity(701, 50m) });
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { product });
        var stockItem = StockItem.Create(1, 701, minimumQuantity: 0, maximumQuantity: null).Value;
        stockItem.Increase(20m).IsSuccess.Should().BeTrue();
        _stockItemRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>()).Returns(new[] { stockItem });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Scenarios.Should().OnlyContain(s => s.StockPlan.Count == 1);
        foreach (var scenario in result.Value.Scenarios)
        {
            var item = scenario.StockPlan.Single();
            item.ProductId.Should().Be(701);
            item.ProductName.Should().Be("Cerveja");
            item.CurrentStock.Should().Be(20m);
            item.RevenueShare.Should().Be(1m); // único produto no mix -> 100% da receita.
        }
    }

    [Fact]
    public async Task Handle_NoSoldQuantities_StockPlanIsEmptyForEveryScenario()
    {
        var query = new GetScenariosQuery(
            BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 8, DesiredProfit: 1000m,
            PessimisticMargin: 0.3m, NormalMargin: 0.3m, OptimisticMargin: 0.3m);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Scenarios.Should().OnlyContain(s => s.StockPlan.Count == 0);
    }
}
