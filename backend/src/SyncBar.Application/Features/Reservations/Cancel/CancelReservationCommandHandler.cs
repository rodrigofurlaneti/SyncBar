using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Reservations.Cancel;

internal sealed class CancelReservationCommandHandler : BaseCommandHandler<CancelReservationCommand>
{
    private readonly ITableReservationRepository _reservationRepository;
    private readonly IDiningTableRepository _diningTableRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelReservationCommandHandler(
        ITableReservationRepository reservationRepository,
        IDiningTableRepository diningTableRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _reservationRepository = reservationRepository;
        _diningTableRepository = diningTableRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(CancelReservationCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CancelReservationCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário ou cliente cancelando a reserva, preencha:
                // userIdBox.Value = request.UserId;

                var reservation = await _reservationRepository.GetByIdForUpdateAsync(request.ReservationId, cancellationToken);
                if (reservation is null || !reservation.IsActive)
                    return Result.Failure(new Error("TableReservation.NotFound", "Reservation not found."));

                var wasConfirmedTableId = reservation.DiningTableId;

                var cancelled = reservation.Cancel();
                if (cancelled.IsFailure)
                    return cancelled;

                // Libera a mesa que estava comprometida para essa reserva.
                if (wasConfirmedTableId.HasValue)
                {
                    var table = await _diningTableRepository.GetByIdForUpdateAsync(wasConfirmedTableId.Value, cancellationToken);
                    table?.ChangeStatus(TableStatusIds.Livre);
                }

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}