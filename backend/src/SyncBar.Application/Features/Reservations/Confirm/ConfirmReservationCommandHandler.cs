using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Reservations.Confirm;

internal sealed class ConfirmReservationCommandHandler : BaseCommandHandler<ConfirmReservationCommand>
{
    private readonly ITableReservationRepository _reservationRepository;
    private readonly IDiningTableRepository _diningTableRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmReservationCommandHandler(
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

    public override async Task<Result> Handle(ConfirmReservationCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ConfirmReservationCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário (ex: recepcionista) confirmando a reserva, preencha:

                var reservation = await _reservationRepository.GetByIdForUpdateAsync(request.ReservationId, cancellationToken);
                if (reservation is null || !reservation.IsActive)
                    return Result.Failure(new Error("TableReservation.NotFound", "Reservation not found."));

                var table = await _diningTableRepository.GetByIdForUpdateAsync(request.DiningTableId, cancellationToken);
                if (table is null || !table.IsActive)
                    return Result.Failure(new Error("DiningTable.NotFound", "Dining table not found."));

                var confirmed = reservation.Confirm(request.DiningTableId);
                if (confirmed.IsFailure)
                    return confirmed;

                // Marca a mesa como reservada para o salão saber que ela está comprometida.
                table.ChangeStatus(TableStatusIds.Reservada);

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}