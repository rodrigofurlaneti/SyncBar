using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Reservations.Cancel;

public sealed record CancelReservationCommand(long ReservationId) : ICommand;
