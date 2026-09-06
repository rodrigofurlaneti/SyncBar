using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Integrations.Ifood.Financial;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.IFood.Financial;

public sealed class GetIfoodFinancialSummaryQueryHandlerTests
{
    private readonly IIfoodFinancialEventRepository _financialEventRepository = Substitute.For<IIfoodFinancialEventRepository>();
    private readonly IIfoodSettlementRepository _settlementRepository = Substitute.For<IIfoodSettlementRepository>();
    private readonly IIfoodOrderRepository _orderRepository = Substitute.For<IIfoodOrderRepository>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetIfoodFinancialSummaryQueryHandler _handler;
    private static readonly DateTime Now = new(2026, 9, 3, 10, 0, 0);

    public GetIfoodFinancialSummaryQueryHandlerTests()
    {
        _handler = new GetIfoodFinancialSummaryQueryHandler(
            _financialEventRepository, _settlementRepository, _orderRepository, _timeProvider, _logRepository, _unitOfWork);

        _timeProvider.GetUtcNow().Returns(new DateTimeOffset(Now, TimeSpan.Zero));
        _timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Utc);
    }

    private static IfoodFinancialEvent CreateEvent(decimal amount, bool hasTransferImpact, string? referenceType = null, string? referenceId = null) =>
        IfoodFinancialEvent.Create(
            1, "evt-1", "Venda", null, null, amount, hasTransferImpact, Now, Now.AddDays(-30), Now,
            null, referenceType, referenceId, "{}").Value;

    private static IfoodSettlement CreateSettlement(decimal amount) =>
        IfoodSettlement.Create(1, "settle-1", "TRANSFER", null, amount, "SUCCEED", Now, null, null, null, "{}").Value;

    [Fact]
    public async Task Handle_NoExplicitRange_ShouldDefaultToLast30DaysEndingNow()
    {
        var query = new GetIfoodFinancialSummaryQuery(1, null, null);
        _financialEventRepository.GetByBranchAndPeriodAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _settlementRepository.GetByBranchAndPeriodAsync(1, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PeriodEnd.Should().Be(Now);
        result.Value.PeriodStart.Should().Be(Now.AddDays(-30));
    }

    [Fact]
    public async Task Handle_EventLinkedToOrder_ShouldResolveLinkedIfoodOrderId()
    {
        var query = new GetIfoodFinancialSummaryQuery(1, Now.AddDays(-10), Now);
        var evt = CreateEvent(100m, true, "ORDER", "ifood-order-1");
        _financialEventRepository.GetByBranchAndPeriodAsync(1, query.From!.Value, query.To!.Value, Arg.Any<CancellationToken>())
            .Returns([evt]);
        _settlementRepository.GetByBranchAndPeriodAsync(1, query.From!.Value, query.To!.Value, Arg.Any<CancellationToken>())
            .Returns([]);
        var linkedOrder = IfoodOrder.Create(10, 1, "ifood-order-1", null, "merchant-1", "DELIVERY", null, "IMMEDIATE", null, Now, false).Value;
        _orderRepository.GetByIfoodOrderIdAsync("ifood-order-1", Arg.Any<CancellationToken>()).Returns(linkedOrder);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Events.Should().ContainSingle(e => e.LinkedIfoodOrderId == linkedOrder.Id);
    }

    [Fact]
    public async Task Handle_EventsAndSettlementsMatch_ShouldReportNoDiscrepancy()
    {
        var query = new GetIfoodFinancialSummaryQuery(1, Now.AddDays(-10), Now);
        _financialEventRepository.GetByBranchAndPeriodAsync(1, query.From!.Value, query.To!.Value, Arg.Any<CancellationToken>())
            .Returns([CreateEvent(100m, true)]);
        _settlementRepository.GetByBranchAndPeriodAsync(1, query.From!.Value, query.To!.Value, Arg.Any<CancellationToken>())
            .Returns([CreateSettlement(100m)]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasDiscrepancy.Should().BeFalse();
        result.Value.TotalFinancialEventsWithTransferImpact.Should().Be(100m);
        result.Value.TotalSettlements.Should().Be(100m);
    }

    [Fact]
    public async Task Handle_EventsAndSettlementsDivergeBeyondTolerance_ShouldReportDiscrepancy()
    {
        var query = new GetIfoodFinancialSummaryQuery(1, Now.AddDays(-10), Now);
        _financialEventRepository.GetByBranchAndPeriodAsync(1, query.From!.Value, query.To!.Value, Arg.Any<CancellationToken>())
            .Returns([CreateEvent(100m, true)]);
        _settlementRepository.GetByBranchAndPeriodAsync(1, query.From!.Value, query.To!.Value, Arg.Any<CancellationToken>())
            .Returns([CreateSettlement(50m)]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasDiscrepancy.Should().BeTrue();
        result.Value.DiscrepancyAmount.Should().Be(50m);
    }

    [Fact]
    public async Task Handle_EventWithoutTransferImpact_ShouldBeExcludedFromDiscrepancyCalculation()
    {
        var query = new GetIfoodFinancialSummaryQuery(1, Now.AddDays(-10), Now);
        _financialEventRepository.GetByBranchAndPeriodAsync(1, query.From!.Value, query.To!.Value, Arg.Any<CancellationToken>())
            .Returns([CreateEvent(100m, false)]);
        _settlementRepository.GetByBranchAndPeriodAsync(1, query.From!.Value, query.To!.Value, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalFinancialEventsWithTransferImpact.Should().Be(0m);
        result.Value.HasDiscrepancy.Should().BeFalse();
    }
}
