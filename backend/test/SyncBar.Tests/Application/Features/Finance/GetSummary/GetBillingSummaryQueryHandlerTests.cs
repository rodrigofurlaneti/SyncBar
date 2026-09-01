using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Finance.GetSummary;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Finance.GetSummary;

public sealed class GetBillingSummaryQueryHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IOperatingCostRepository _costRepository = Substitute.For<IOperatingCostRepository>();
    private readonly IRevenueTargetRepository _targetRepository = Substitute.For<IRevenueTargetRepository>();
    private readonly IStockMovementRepository _stockMovementRepository = Substitute.For<IStockMovementRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetBillingSummaryQueryHandler _handler;

    public GetBillingSummaryQueryHandlerTests()
    {
        _handler = new GetBillingSummaryQueryHandler(
            _saleRepository, _costRepository, _targetRepository, _stockMovementRepository,
            _logRepository, _unitOfWork);
    }

    private static Sale CreateSale(decimal subtotalAmount, long saleNumber)
        => Sale.Create(
            branchId: 1, customerOrderId: 100, cashSessionId: 1, employeeId: 1,
            saleNumber: saleNumber, subtotalAmount: subtotalAmount, discountAmount: 0m, serviceFeeAmount: 0m).Value;

    private static OperatingCost CreateCost(long costTypeId, string description, decimal amount)
        => OperatingCost.Create(
            branchId: 1, costTypeId: costTypeId, description: description, amount: amount,
            referenceYear: 2026, referenceMonth: 9).Value;

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task Handle_InvalidReferenceMonth_ShouldReturnFailureWithoutQueryingAnyRepository(int invalidMonth)
    {
        var query = new GetBillingSummaryQuery(BranchId: 1, ReferenceYear: 2026, ReferenceMonth: invalidMonth);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BillingSummary.InvalidMonth");
        await _saleRepository.DidNotReceive().GetByBranchAndPeriodAsync(
            Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _costRepository.DidNotReceive().GetByBranchAndMonthAsync(
            Arg.Any<long>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _targetRepository.DidNotReceive().GetByBranchAndMonthAsync(
            Arg.Any<long>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _stockMovementRepository.DidNotReceive().GetSaleCostAsync(
            Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        // Só o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoSalesCostsOrTarget_ShouldReturnZeroedSummaryWithNullAttainmentRate()
    {
        var query = new GetBillingSummaryQuery(BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 9);

        _saleRepository.GetByBranchAndPeriodAsync(query.BranchId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Sale>());
        _costRepository.GetByBranchAndMonthAsync(query.BranchId, query.ReferenceYear, query.ReferenceMonth, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<OperatingCost>());
        _targetRepository.GetByBranchAndMonthAsync(query.BranchId, query.ReferenceYear, query.ReferenceMonth, Arg.Any<CancellationToken>())
            .Returns((RevenueTarget?)null);
        _stockMovementRepository.GetSaleCostAsync(query.BranchId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(0m);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.ReferenceYear.Should().Be(2026);
        response.ReferenceMonth.Should().Be(9);
        response.Revenue.Should().Be(0m);
        response.SalesCount.Should().Be(0);
        response.CostOfGoodsSold.Should().Be(0m);
        response.FixedCosts.Should().Be(0m);
        response.VariableCosts.Should().Be(0m);
        response.TotalCosts.Should().Be(0m);
        response.NetResult.Should().Be(0m);
        response.TargetAmount.Should().BeNull();
        response.TargetAttainmentRate.Should().BeNull();
        response.Costs.Should().BeEmpty();
        response.DailyRevenue.Should().BeEmpty();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSalesCostsAndTarget_ShouldAggregateGroupCostsAndComputeAttainmentRate()
    {
        var query = new GetBillingSummaryQuery(BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 9);

        var sale1 = CreateSale(subtotalAmount: 300m, saleNumber: 1001);
        var sale2 = CreateSale(subtotalAmount: 200m, saleNumber: 1002);
        // Sale.SoldAt é sempre DateTime.Now (sem fábrica que permita controlar a data) — as duas
        // vendas caem no mesmo dia corrido do teste, então o agrupamento diário produz uma única
        // entrada com a soma das duas.
        var fixedCost = CreateCost(CostTypeIds.Fixo, "Aluguel", 800m);
        var variableCost = CreateCost(CostTypeIds.Variavel, "Comissão de vendas", 200m);
        var target = RevenueTarget.Create(query.BranchId, query.ReferenceYear, query.ReferenceMonth, 1000m).Value;

        _saleRepository.GetByBranchAndPeriodAsync(query.BranchId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([sale1, sale2]);
        // Retornado fora de ordem de propósito para exercitar o OrderBy(CostTypeId).ThenBy(Description) do handler.
        _costRepository.GetByBranchAndMonthAsync(query.BranchId, query.ReferenceYear, query.ReferenceMonth, Arg.Any<CancellationToken>())
            .Returns([variableCost, fixedCost]);
        _targetRepository.GetByBranchAndMonthAsync(query.BranchId, query.ReferenceYear, query.ReferenceMonth, Arg.Any<CancellationToken>())
            .Returns(target);
        _stockMovementRepository.GetSaleCostAsync(query.BranchId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(150m);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.Revenue.Should().Be(500m); // 300 + 200
        response.SalesCount.Should().Be(2);
        response.CostOfGoodsSold.Should().Be(150m);
        response.FixedCosts.Should().Be(800m);
        response.VariableCosts.Should().Be(200m);
        response.TotalCosts.Should().Be(1150m); // 800 + 200 + 150
        response.NetResult.Should().Be(-650m); // 500 - 1150
        response.TargetAmount.Should().Be(1000m);
        response.TargetAttainmentRate.Should().Be(0.5m); // round(500 / 1000, 4)

        response.Costs.Should().HaveCount(2);
        response.Costs.ElementAt(0).CostTypeId.Should().Be(CostTypeIds.Fixo);
        response.Costs.ElementAt(0).Description.Should().Be("Aluguel");
        response.Costs.ElementAt(0).Amount.Should().Be(800m);
        response.Costs.ElementAt(1).CostTypeId.Should().Be(CostTypeIds.Variavel);
        response.Costs.ElementAt(1).Description.Should().Be("Comissão de vendas");
        response.Costs.ElementAt(1).Amount.Should().Be(200m);

        response.DailyRevenue.Should().ContainSingle();
        var daily = response.DailyRevenue.Single();
        daily.Day.Should().Be(DateTime.Now.Day);
        daily.Amount.Should().Be(500m);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
