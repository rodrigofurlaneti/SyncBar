using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Reservations.Create;

public sealed record CreateReservationCommand(
    long BranchId,
    string CustomerName,
    string? CustomerPhone,
    int PartySize,
    DateTime ReservedFor,
    string? Notes) : ICommand<long>;
