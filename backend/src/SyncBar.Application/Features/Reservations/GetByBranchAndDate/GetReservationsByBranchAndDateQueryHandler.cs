using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Reservations.GetByBranchAndDate;

internal sealed class GetReservationsByBranchAndDateQueryHandler(
    ITableReservationRepository reservationRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetReservationsByBranchAndDateQuery, IReadOnlyCollection<ReservationResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<ReservationResponse>>> Handle(
        GetReservationsByBranchAndDateQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetReservationsByBranchAndDateQueryHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário (ex: recepcionista) consultando as reservas, preencha:
                // userIdBox.Value = request.UserId;

                var reservations = await reservationRepository.GetByBranchAndDateAsync(
                    request.BranchId, request.From, request.To, cancellationToken);

                IReadOnlyCollection<ReservationResponse> response = reservations
                    .Select(r => new ReservationResponse(
                        r.Id, r.BranchId, r.DiningTableId, r.CustomerName, r.CustomerPhone,
                        r.PartySize, r.ReservedFor, r.ReservationStatusId, r.Notes))
                    .ToList();

                return Result.Success(response);
            });
    }
}