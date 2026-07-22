using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Reservations.Create;

internal sealed class CreateReservationCommandHandler(ITableReservationRepository reservationRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateReservationCommand, long>
{
    public async Task<Result<long>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = TableReservation.Create(
            request.BranchId, null, request.CustomerName, request.CustomerPhone,
            request.PartySize, request.ReservedFor, request.Notes);
        if (reservation.IsFailure)
            return Result.Failure<long>(reservation.Error);

        await reservationRepository.AddAsync(reservation.Value, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(reservation.Value.Id);
    }
}
