using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Reservations.Confirm;

public sealed record ConfirmReservationCommand(long ReservationId, long DiningTableId) : ICommand;
