using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Reservations.GetByBranchAndDate;

internal sealed class GetReservationsByBranchAndDateQueryHandler(ITableReservationRepository reservationRepository)
    : IQueryHandler<GetReservationsByBranchAndDateQuery, IReadOnlyCollection<ReservationResponse>>
{
    public async Task<Result<IReadOnlyCollection<ReservationResponse>>> Handle(
        GetReservationsByBranchAndDateQuery request, CancellationToken cancellationToken)
    {
        var reservations = await reservationRepository.GetByBranchAndDateAsync(
            request.BranchId, request.From, request.To, cancellationToken);

        IReadOnlyCollection<ReservationResponse> response = reservations
            .Select(r => new ReservationResponse(
                r.Id, r.BranchId, r.DiningTableId, r.CustomerName, r.CustomerPhone,
                r.PartySize, r.ReservedFor, r.ReservationStatusId, r.Notes))
            .ToList();

        return Result.Success(response);
    }
}
