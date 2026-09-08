using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Shift.GetHistory;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Shift.GetHistory;

public sealed class GetShiftClosingHistoryQueryHandlerTests
{
    private readonly IShiftClosingRepository _shiftClosingRepository = Substitute.For<IShiftClosingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetShiftClosingHistoryQueryHandler _handler;

    public GetShiftClosingHistoryQueryHandlerTests()
    {
        _handler = new GetShiftClosingHistoryQueryHandler(_shiftClosingRepository, _logRepository, _unitOfWork);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task Handle_InvalidReferenceMonth_ShouldReturnInvalidMonthFailure(int month)
    {
        var query = new GetShiftClosingHistoryQuery(BranchId: 1, ReferenceYear: 2026, ReferenceMonth: month);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ShiftClosingHistory.InvalidMonth");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoShiftsInPeriod_ShouldReturnEmptyCollectionAndQueryTheCorrectPeriod()
    {
        var query = new GetShiftClosingHistoryQuery(BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 8);
        var expectedFrom = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedTo = expectedFrom.AddMonths(1);

        _shiftClosingRepository.GetHistoryByBranchAsync(query.BranchId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ShiftClosing>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        await _shiftClosingRepository.Received(1).GetHistoryByBranchAsync(
            query.BranchId, expectedFrom, expectedTo, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleShifts_ShouldOrderByPeriodStartDescending()
    {
        var query = new GetShiftClosingHistoryQuery(BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 8);
        var olderShift = ShiftClosing.Open(query.BranchId, openedByEmployeeId: 10).Value;
        // Garante um PeriodStart distinguível do segundo, já que ShiftClosing.Open usa DateTime.Now
        // internamente e a resolução do relógio do Windows pode não diferenciar chamadas muito próximas.
        await Task.Delay(20);
        var newerShift = ShiftClosing.Open(query.BranchId, openedByEmployeeId: 20).Value;

        _shiftClosingRepository.GetHistoryByBranchAsync(query.BranchId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([olderShift, newerShift]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.ElementAt(0).OpenedByEmployeeId.Should().Be(newerShift.OpenedByEmployeeId);
        result.Value.ElementAt(1).OpenedByEmployeeId.Should().Be(olderShift.OpenedByEmployeeId);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ClosedShift_ShouldMapConsolidatedTotalsToResponse()
    {
        var query = new GetShiftClosingHistoryQuery(BranchId: 1, ReferenceYear: 2026, ReferenceMonth: 8);
        var shift = ShiftClosing.Open(query.BranchId, openedByEmployeeId: 10).Value;
        var session = CashSession.Open(cashRegisterId: 1, openedByEmployeeId: 10, openingAmount: 100m).Value;
        session.Close(closedByEmployeeId: 10, closingAmount: 150m, expectedAmount: 140m).IsSuccess.Should().BeTrue();
        shift.Close(closedByEmployeeId: 10, periodEnd: DateTime.Now, cashSessions: [session], notes: "Turno noturno")
            .IsSuccess.Should().BeTrue();

        _shiftClosingRepository.GetHistoryByBranchAsync(query.BranchId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([shift]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Single();
        response.ShiftClosingStatusId.Should().Be(shift.ShiftClosingStatusId);
        response.CashSessionsCount.Should().Be(shift.CashSessionsCount);
        response.TotalOpeningAmount.Should().Be(shift.TotalOpeningAmount);
        response.TotalExpectedAmount.Should().Be(shift.TotalExpectedAmount);
        response.TotalRealizedAmount.Should().Be(shift.TotalRealizedAmount);
        response.TotalDifferenceAmount.Should().Be(shift.TotalDifferenceAmount);
        response.Notes.Should().Be("Turno noturno");
    }
}
