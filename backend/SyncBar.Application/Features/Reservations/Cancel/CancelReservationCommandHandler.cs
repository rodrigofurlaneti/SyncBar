using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Reservations.Cancel;

internal sealed class CancelReservationCommandHandler(
    ITableReservationRepository reservationRepository,
    IDiningTableRepository diningTableRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelReservationCommand>
{
    public async Task<Result> Handle(CancelReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await reservationRepository.GetByIdForUpdateAsync(request.ReservationId, cancellationToken);
        if (reservation is null || !reservation.IsActive)
            return Result.Failure(new Error("TableReservation.NotFound", "Reservation not found."));

        var wasConfirmedTableId = reservation.DiningTableId;

        var cancelled = reservation.Cancel();
        if (cancelled.IsFailure)
            return cancelled;

        // Libera a mesa que estava comprometida para essa reserva.
        if (wasConfirmedTableId.HasValue)
        {
            var table = await diningTableRepository.GetByIdForUpdateAsync(wasConfirmedTableId.Value, cancellationToken);
            table?.ChangeStatus(TableStatusIds.Livre);
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
