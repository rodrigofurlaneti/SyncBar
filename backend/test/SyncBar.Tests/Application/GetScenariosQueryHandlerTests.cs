using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Finance.GetScenarios;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class GetScenariosQueryHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IOperatingCostRepository _costRepository = Substitute.For<IOperatingCostRepository>();
    private readonly IStockMovementRepository _stockMovementRepository = Substitute.For<IStockMovementRepository>();
    private readonly IStockItemRepository _stockItemRepository = Substitute.For<IStockItemRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();

    private GetScenariosQueryHandler CreateHandler()
        => new(_saleRepository, _costRepository, _stockMovementRepository, _stockItemRepository, _productRepository);

    private static T WithId<T>(T entity, long id) where T : SyncBar.Domain.Primitives.Entity
    {
        typeof(SyncBar.Domain.Primitives.Entity).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }

    [Fact]
    public async Task Handle_ShouldComputeBreakEvenAndStockPlanFromRealMix()
    {
        // Receita 10.000 | CMV 3.000 | variaveis 1.000 → margem 60% | fixos 6.000
        // Break-even = 6000/0.6 = 10.000 | alvo (lucro 3.000) = 9000/0.6 = 15.000
        var sale = Sale.Create(1, 1, 1, 1, 1, 10000m, 0m, 0m).Value;
        _saleRepository.GetByBranchAndPeriodAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<Sale> { sale });

        _costRepository.GetByBranchAndMonthAsync(1, 2026, 7, Arg.Any<CancellationToken>())
            .Returns(new List<OperatingCost>
            {
                OperatingCost.Create(1, CostTypeIds.Fixo, "Aluguel", 6000m, 2026, 7).Value,
                OperatingCost.Create(1, CostTypeIds.Variavel, "Taxas", 1000m, 2026, 7).Value,
            });

        _stockMovementRepository.GetSaleCostAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(3000m);

        // Mix: produto 1 (cerveja R$10) vendeu 800 → 8.000 (80%); produto 2 (agua R$5) vendeu 400 → 2.000 (20%)
        _stockMovementRepository.GetSaleQuantitiesByProductAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductQuantity> { new(1, 800m), new(2, 400m) });

        var beer = WithId(Product.Create(1, 1, 1, "Cerveja", null, null, 10m, 4m, true, null).Value, 1);
        var water = WithId(Product.Create(1, 1, 1, "Agua", null, null, 5m, 1m, true, null).Value, 2);
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { beer, water });

        var beerStock = StockItem.Create(1, 1, 10, null).Value;
        beerStock.Increase(500);
        _stockItemRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<StockItem> { beerStock });

        var result = await CreateHandler().Handle(
            new GetScenariosQuery(1, 2026, 7, DesiredProfit: 3000m, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HistoricalMarginRate.Should().Be(0.60m);

        var normal = result.Value.Scenarios.Single(s => s.Name == "Normal");
        normal.MarginRate.Should().Be(0.60m);
        normal.BreakEvenRevenue.Should().Be(10000m);
        normal.TargetRevenue.Should().Be(15000m);
        normal.DailyTarget.Should().Be(Math.Round(15000m / 31, 2));

        // Cerveja: 80% de 15.000 = 12.000 → 1.200 un; tem 500 → comprar 700.
        var beerPlan = normal.StockPlan.Single(p => p.ProductId == 1);
        beerPlan.EstimatedUnits.Should().Be(1200m);
        beerPlan.CurrentStock.Should().Be(500m);
        beerPlan.UnitsToBuy.Should().Be(700m);

        // Pessimista exige mais faturamento que o normal; otimista, menos.
        var pessimistic = result.Value.Scenarios.Single(s => s.Name == "Pessimista");
        var optimistic = result.Value.Scenarios.Single(s => s.Name == "Otimista");
        pessimistic.TargetRevenue.Should().BeGreaterThan(normal.TargetRevenue);
        optimistic.TargetRevenue.Should().BeLessThan(normal.TargetRevenue);
    }

    [Fact]
    public async Task Handle_WithoutHistory_ShouldUseDefaultMargin()
    {
        _saleRepository.GetByBranchAndPeriodAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<Sale>());
        _costRepository.GetByBranchAndMonthAsync(1, 2026, 8, Arg.Any<CancellationToken>())
            .Returns(new List<OperatingCost>
            {
                OperatingCost.Create(1, CostTypeIds.Fixo, "Aluguel", 3000m, 2026, 8).Value,
            });
        _stockMovementRepository.GetSaleCostAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(0m);
        _stockMovementRepository.GetSaleQuantitiesByProductAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductQuantity>());
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>());
        _stockItemRepository.GetByBranchAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<StockItem>());

        var result = await CreateHandler().Handle(
            new GetScenariosQuery(1, 2026, 8, 0m, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HistoricalMarginRate.Should().BeNull();
        result.Value.Scenarios.Single(s => s.Name == "Normal").MarginRate.Should().Be(0.30m);
        result.Value.Scenarios.Single(s => s.Name == "Normal").BreakEvenRevenue.Should().Be(10000m);
        result.Value.Scenarios.Single(s => s.Name == "Normal").StockPlan.Should().BeEmpty();
    }
}
