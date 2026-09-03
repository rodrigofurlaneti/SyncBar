using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Reservations.Confirm;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Reservations.Confirm;

public sealed class ConfirmReservationCommandHandlerTests
{
    private readonly ITableReservationRepository _reservationRepository = Substitute.For<ITableReservationRepository>();
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ConfirmReservationCommandHandler _handler;

    public ConfirmReservationCommandHandlerTests()
    {
        _handler = new ConfirmReservationCommandHandler(_reservationRepository, _diningTableRepository, _logRepository, _unitOfWork);
    }

    private static TableReservation CreatePendingReservation()
        => TableReservation.Create(1, null, "Maria Silva", null, 4, DateTime.Now.AddDays(1), null).Value;

    private static DiningTable CreateActiveDiningTable(long tableStatusId = TableStatusIds.Livre)
        => DiningTable.Create(1, tableStatusId, 5, 4).Value;

    [Fact]
    public async Task Handle_ReservationNotFound_ShouldReturnFailureWithoutCommitting()
    {
        var command = new ConfirmReservationCommand(ReservationId: 1, DiningTableId: 1);
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
        var command = new ConfirmReservationCommand(ReservationId: 1, DiningTableId: 1);
        _reservationRepository.GetByIdForUpdateAsync(command.ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TableReservation.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DiningTableNotFound_ShouldReturnFailureWithoutCommitting()
    {
        var reservation = CreatePendingReservation();
        var command = new ConfirmReservationCommand(ReservationId: 1, DiningTableId: 1);
        _reservationRepository.GetByIdForUpdateAsync(command.ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);
        _diningTableRepository.GetByIdForUpdateAsync(command.DiningTableId, Arg.Any<CancellationToken>()).Returns((DiningTable?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningTable.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DiningTableInactive_ShouldReturnFailureWithoutCommitting()
    {
        var reservation = CreatePendingReservation();
        var table = CreateActiveDiningTable();
        table.Deactivate();
        var command = new ConfirmReservationCommand(ReservationId: 1, DiningTableId: 1);
        _reservationRepository.GetByIdForUpdateAsync(command.ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);
        _diningTableRepository.GetByIdForUpdateAsync(command.DiningTableId, Arg.Any<CancellationToken>()).Returns(table);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningTable.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReservationAlreadyConfirmed_ShouldReturnFailureWithoutCommitting()
    {
        var reservation = CreatePendingReservation();
        reservation.Confirm(diningTableId: 99); // já confirmada — não está mais Pending
        var table = CreateActiveDiningTable();
        var command = new ConfirmReservationCommand(ReservationId: 1, DiningTableId: 1);
        _reservationRepository.GetByIdForUpdateAsync(command.ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);
        _diningTableRepository.GetByIdForUpdateAsync(command.DiningTableId, Arg.Any<CancellationToken>()).Returns(table);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TableReservation.NotPending");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldConfirmReservationAndMarkTableAsReserved()
    {
        var reservation = CreatePendingReservation();
        var table = CreateActiveDiningTable();
        var command = new ConfirmReservationCommand(ReservationId: 1, DiningTableId: 1);
        _reservationRepository.GetByIdForUpdateAsync(command.ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);
        _diningTableRepository.GetByIdForUpdateAsync(command.DiningTableId, Arg.Any<CancellationToken>()).Returns(table);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        reservation.DiningTableId.Should().Be(command.DiningTableId);
        reservation.ReservationStatusId.Should().Be(ReservationStatusIds.Confirmed);
        table.TableStatusId.Should().Be(TableStatusIds.Reservada);
        // Commit explícito do handler + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
