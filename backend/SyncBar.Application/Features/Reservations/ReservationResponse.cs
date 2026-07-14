namespace SyncBar.Application.Features.Reservations;

public sealed record ReservationResponse(
    long Id,
    long BranchId,
    long? DiningTableId,
    string CustomerName,
    string? CustomerPhone,
    int PartySize,
    DateTime ReservedFor,
    long ReservationStatusId,
    string? Notes);
