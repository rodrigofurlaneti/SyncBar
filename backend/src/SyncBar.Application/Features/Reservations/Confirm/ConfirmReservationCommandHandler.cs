using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Reservations.Confirm;

internal sealed class ConfirmReservationCommandHandler(
    ITableReservationRepository reservationRepository,
    IDiningTableRepository diningTableRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<ConfirmReservationCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(ConfirmReservationCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ConfirmReservationCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário (ex: recepcionista) confirmando a reserva, preencha:
                // userIdBox.Value = request.UserId;

                var reservation = await reservationRepository.GetByIdForUpdateAsync(request.ReservationId, cancellationToken);
                if (reservation is null || !reservation.IsActive)
                    return Result.Failure(new Error("TableReservation.NotFound", "Reservation not found."));

                var table = await diningTableRepository.GetByIdForUpdateAsync(request.DiningTableId, cancellationToken);
                if (table is null || !table.IsActive)
                    return Result.Failure(new Error("DiningTable.NotFound", "Dining table not found."));

                var confirmed = reservation.Confirm(request.DiningTableId);
                if (confirmed.IsFailure)
                    return confirmed;

                // Marca a mesa como reservada para o salão saber que ela está comprometida.
                table.ChangeStatus(TableStatusIds.Reservada);

                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}