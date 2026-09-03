using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Shift.CloseShift;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Shift.CloseShift;

public sealed class CloseShiftClosingCommandHandlerTests
{
    private readonly IShiftClosingRepository _shiftClosingRepository = Substitute.For<IShiftClosingRepository>();
    private readonly ICashSessionRepository _cashSessionRepository = Substitute.For<ICashSessionRepository>();
    private readonly IShiftClosingSessionRepository _shiftClosingSessionRepository = Substitute.For<IShiftClosingSessionRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CloseShiftClosingCommandHandler _handler;

    public CloseShiftClosingCommandHandlerTests()
    {
        _handler = new CloseShiftClosingCommandHandler(
            _shiftClosingRepository, _cashSessionRepository, _shiftClosingSessionRepository,
            _logRepository, _unitOfWork);
    }

    private static ShiftClosing CreateOpenShift(long branchId = 1)
        => ShiftClosing.Open(branchId, openedByEmployeeId: 10).Value;

    // ShiftClosingSession.Create valida ShiftClosingId > 0 e CashSessionId > 0 (é uma FK real,
    // não um mero valor comparado — diferente do caso de "productId" em outros handlers). Como a
    // fábrica pública do Entity/AggregateRoot não expõe forma de fixar o Id (ele só existiria após
    // o SaveChanges do EF Core), sem isso o loop de consolidação do handler nunca monta nenhum link
    // em teste. Setamos via reflection, imitando o Id que o EF teria atribuído após persistir.
    private static void SetId(Entity entity, long id) =>
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    [Fact]
    public async Task Handle_ShiftNotFound_ShouldReturnFailureWithoutQueryingCashSessions()
    {
        var command = new CloseShiftClosingCommand(ShiftClosingId: 1, ClosedByEmployeeId: 10, Notes: null);
        _shiftClosingRepository.GetByIdForUpdateAsync(command.ShiftClosingId, Arg.Any<CancellationToken>())
            .Returns((ShiftClosing?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ShiftClosing.NotFound");

        await _cashSessionRepository.DidNotReceive().GetByBranchAndPeriodAsync(
            Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShiftInactive_ShouldReturnFailure()
    {
        var shift = CreateOpenShift();
        shift.Deactivate();
        var command = new CloseShiftClosingCommand(ShiftClosingId: 1, ClosedByEmployeeId: 10, Notes: null);
        _shiftClosingRepository.GetByIdForUpdateAsync(command.ShiftClosingId, Arg.Any<CancellationToken>())
            .Returns(shift);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ShiftClosing.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithOpenCashSessionPending_ShouldReturnFailureAndNotPersistLinksOrCommit()
    {
        var shift = CreateOpenShift(branchId: 1);
        var command = new CloseShiftClosingCommand(ShiftClosingId: 1, ClosedByEmployeeId: 9, Notes: null);
        var openSession = CashSession.Open(cashRegisterId: 1, openedByEmployeeId: 10, openingAmount: 100m).Value;

        _shiftClosingRepository.GetByIdForUpdateAsync(command.ShiftClosingId, Arg.Any<CancellationToken>())
            .Returns(shift);
        _cashSessionRepository.GetByBranchAndPeriodAsync(shift.BranchId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { openSession });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ShiftClosing.OpenCashSessionsPending");
        shift.IsOpen().Should().BeTrue();

        await _shiftClosingSessionRepository.DidNotReceive().AddRangeAsync(
            Arg.Any<IEnumerable<ShiftClosingSession>>(), Arg.Any<CancellationToken>());
        // Sem commit explícito nesse ramo: só o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAllCashSessionsClosed_ShouldConsolidateAndPersistLinksAndReturnTotals()
    {
        var shift = CreateOpenShift(branchId: 1);
        SetId(shift, 1);
        var command = new CloseShiftClosingCommand(ShiftClosingId: 1, ClosedByEmployeeId: 9, Notes: "Ok");

        var session1 = CashSession.Open(cashRegisterId: 1, openedByEmployeeId: 10, openingAmount: 100m).Value;
        session1.Close(closedByEmployeeId: 10, closingAmount: 520m, expectedAmount: 500m);
        SetId(session1, 101);

        var session2 = CashSession.Open(cashRegisterId: 2, openedByEmployeeId: 11, openingAmount: 50m).Value;
        session2.Close(closedByEmployeeId: 11, closingAmount: 280m, expectedAmount: 300m);
        SetId(session2, 102);

        _shiftClosingRepository.GetByIdForUpdateAsync(command.ShiftClosingId, Arg.Any<CancellationToken>())
            .Returns(shift);
        _cashSessionRepository.GetByBranchAndPeriodAsync(shift.BranchId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new[] { session1, session2 });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(shift.Id);
        result.Value.CashSessionsCount.Should().Be(2);
        result.Value.TotalOpeningAmount.Should().Be(150m);
        result.Value.TotalExpectedAmount.Should().Be(800m);
        result.Value.TotalRealizedAmount.Should().Be(800m);
        result.Value.TotalDifferenceAmount.Should().Be(0m);
        result.Value.Notes.Should().Be("Ok");

        shift.ShiftClosingStatusId.Should().Be(ShiftClosingStatusIds.Fechado);

        await _shiftClosingSessionRepository.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<ShiftClosingSession>>(links => links.Count() == 2),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNoCashSessionsInPeriod_ShouldCloseWithZeroTotalsAndNoLinks()
    {
        var shift = CreateOpenShift(branchId: 1);
        var command = new CloseShiftClosingCommand(ShiftClosingId: 1, ClosedByEmployeeId: 9, Notes: null);

        _shiftClosingRepository.GetByIdForUpdateAsync(command.ShiftClosingId, Arg.Any<CancellationToken>())
            .Returns(shift);
        _cashSessionRepository.GetByBranchAndPeriodAsync(shift.BranchId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CashSession>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CashSessionsCount.Should().Be(0);
        result.Value.TotalDifferenceAmount.Should().Be(0m);

        await _shiftClosingSessionRepository.DidNotReceive().AddRangeAsync(
            Arg.Any<IEnumerable<ShiftClosingSession>>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShiftAlreadyClosed_ShouldPropagateDomainFailureWithoutExplicitCommit()
    {
        var shift = CreateOpenShift(branchId: 1);
        shift.Close(closedByEmployeeId: 10, DateTime.Now, Array.Empty<CashSession>(), notes: null);

        var command = new CloseShiftClosingCommand(ShiftClosingId: 1, ClosedByEmployeeId: 9, Notes: null);
        _shiftClosingRepository.GetByIdForUpdateAsync(command.ShiftClosingId, Arg.Any<CancellationToken>())
            .Returns(shift);
        _cashSessionRepository.GetByBranchAndPeriodAsync(shift.BranchId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CashSession>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ShiftClosing.NotOpen");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
