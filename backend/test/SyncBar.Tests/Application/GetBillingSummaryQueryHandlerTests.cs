using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Finance.GetSummary;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class GetBillingSummaryQueryHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IOperatingCostRepository _costRepository = Substitute.For<IOperatingCostRepository>();
    private readonly IRevenueTargetRepository _targetRepository = Substitute.For<IRevenueTargetRepository>();
    private readonly IStockMovementRepository _stockMovementRepository = Substitute.For<IStockMovementRepository>();

    private GetBillingSummaryQueryHandler CreateHandler()
        => new(_saleRepository, _costRepository, _targetRepository, _stockMovementRepository);

    [Fact]
    public async Task Handle_ShouldComputeRevenueCostsResultAndTarget()
    {
        // Receita 1000 (2 vendas), CMV 150, fixos 300, variaveis 100 → resultado 450; meta 2000 → 50%.
        var sale1 = Sale.Create(1, 1, 1, 1, 1, 600m, 0m, 0m).Value;
        var sale2 = Sale.Create(1, 2, 1, 1, 2, 400m, 0m, 0m).Value;
        _saleRepository.GetByBranchAndPeriodAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<Sale> { sale1, sale2 });

        _costRepository.GetByBranchAndMonthAsync(1, 2026, 7, Arg.Any<CancellationToken>())
            .Returns(new List<OperatingCost>
            {
                OperatingCost.Create(1, CostTypeIds.Fixo, "Aluguel", 300m, 2026, 7).Value,
                OperatingCost.Create(1, CostTypeIds.Variavel, "Comissões", 100m, 2026, 7).Value,
            });

        _targetRepository.GetByBranchAndMonthAsync(1, 2026, 7, Arg.Any<CancellationToken>())
            .Returns(RevenueTarget.Create(1, 2026, 7, 2000m).Value);

        _stockMovementRepository.GetSaleCostAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(150m);

        var result = await CreateHandler().Handle(new GetBillingSummaryQuery(1, 2026, 7), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Revenue.Should().Be(1000m);
        result.Value.SalesCount.Should().Be(2);
        result.Value.CostOfGoodsSold.Should().Be(150m);
        result.Value.FixedCosts.Should().Be(300m);
        result.Value.VariableCosts.Should().Be(100m);
        result.Value.TotalCosts.Should().Be(550m);
        result.Value.NetResult.Should().Be(450m);
        result.Value.TargetAmount.Should().Be(2000m);
        result.Value.TargetAttainmentRate.Should().Be(0.5m);
    }

    [Fact]
    public async Task Handle_WithoutTarget_ShouldReturnNullAttainment()
    {
        _saleRepository.GetByBranchAndPeriodAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<Sale>());
        _costRepository.GetByBranchAndMonthAsync(1, 2026, 7, Arg.Any<CancellationToken>())
            .Returns(new List<OperatingCost>());
        _targetRepository.GetByBranchAndMonthAsync(1, 2026, 7, Arg.Any<CancellationToken>())
            .Returns((RevenueTarget?)null);
        _stockMovementRepository.GetSaleCostAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(0m);

        var result = await CreateHandler().Handle(new GetBillingSummaryQuery(1, 2026, 7), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TargetAmount.Should().BeNull();
        result.Value.TargetAttainmentRate.Should().BeNull();
        result.Value.NetResult.Should().Be(0m);
    }
}
