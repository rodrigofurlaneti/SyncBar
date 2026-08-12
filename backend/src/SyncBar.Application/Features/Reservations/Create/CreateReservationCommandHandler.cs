using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Reservations.Create;

internal sealed class CreateReservationCommandHandler(
    ITableReservationRepository reservationRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<CreateReservationCommand, long>(logRepository, unitOfWork)
{
    public override async Task<Result<long>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CreateReservationCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário (ex: cliente via app ou recepcionista) criando a reserva, preencha:
                // userIdBox.Value = request.UserId;

                var reservation = TableReservation.Create(
                    request.BranchId, null, request.CustomerName, request.CustomerPhone,
                    request.PartySize, request.ReservedFor, request.Notes);

                if (reservation.IsFailure)
                    return Result.Failure<long>(reservation.Error);

                await reservationRepository.AddAsync(reservation.Value, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(reservation.Value.Id);
            });
    }
}