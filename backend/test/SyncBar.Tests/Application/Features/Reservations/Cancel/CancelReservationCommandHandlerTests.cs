using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Reservations.Cancel;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Reservations.Cancel;

public sealed class CancelReservationCommandHandlerTests
{
    private readonly ITableReservationRepository _reservationRepository = Substitute.For<ITableReservationRepository>();
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CancelReservationCommandHandler _handler;

    public CancelReservationCommandHandlerTests()
    {
        _handler = new CancelReservationCommandHandler(_reservationRepository, _diningTableRepository, _logRepository, _unitOfWork);
    }

    private static TableReservation CreatePendingReservation()
        => TableReservation.Create(1, null, "Maria Silva", null, 4, DateTime.Now.AddDays(1), null).Value;

    private static DiningTable CreateActiveDiningTable(long tableStatusId = TableStatusIds.Reservada)
        => DiningTable.Create(1, tableStatusId, 5, 4).Value;

    [Fact]
    public async Task Handle_ReservationNotFound_ShouldReturnFailureWithoutCommitting()
    {
        var command = new CancelReservationCommand(ReservationId: 1);
        _reservationRepository.GetByIdForUpdateAsync(command.ReservationId, Arg.Any<CancellationToken>()).Returns((TableReservation?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TableReservation.NotFound");
        // Nenhum commit explícito do handler; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReservationInactive_ShouldReturnFailureWithoutCommitting()
    {
        var reservation = CreatePendingReservation();
        reservation.Deactivate();
        var command = new CancelReservationCommand(ReservationId: 1);
        _reservationRepository.GetByIdForUpdateAsync(command.ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TableReservation.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReservationAlreadySeated_ShouldReturnFailureWithoutCommitting()
    {
        var reservation = CreatePendingReservation();
        reservation.Confirm(diningTableId: 1);
        reservation.MarkSeated(); // Cancel() falha para reserva Seated
        var command = new CancelReservationCommand(ReservationId: 1);
        _reservationRepository.GetByIdForUpdateAsync(command.ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TableReservation.CannotCancel");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequestWithConfirmedTable_ShouldCancelReservationAndFreeTable()
    {
        var reservation = CreatePendingReservation();
        reservation.Confirm(diningTableId: 1); // define DiningTableId e status Confirmed
        var table = CreateActiveDiningTable();
        var command = new CancelReservationCommand(ReservationId: 1);
        _reservationRepository.GetByIdForUpdateAsync(command.ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);
        _diningTableRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(table);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        reservation.ReservationStatusId.Should().Be(ReservationStatusIds.Cancelled);
        table.TableStatusId.Should().Be(TableStatusIds.Livre);
        // Commit explícito do handler + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequestWithoutConfirmedTable_ShouldCancelReservationWithoutTouchingDiningTableRepository()
    {
        var reservation = CreatePendingReservation(); // nunca confirmada — DiningTableId continua null
        var command = new CancelReservationCommand(ReservationId: 1);
        _reservationRepository.GetByIdForUpdateAsync(command.ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        reservation.ReservationStatusId.Should().Be(ReservationStatusIds.Cancelled);
        await _diningTableRepository.DidNotReceive().GetByIdForUpdateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
