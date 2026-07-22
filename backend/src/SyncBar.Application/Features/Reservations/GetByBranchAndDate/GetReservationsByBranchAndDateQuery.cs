using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Reservations.GetByBranchAndDate;

public sealed record GetReservationsByBranchAndDateQuery(long BranchId, DateTime From, DateTime To)
    : IQuery<IReadOnlyCollection<ReservationResponse>>;
