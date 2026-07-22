using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Cash.RegisterMovement;

internal sealed class RegisterCashMovementCommandHandler(
    ICashSessionRepository cashSessionRepository,
    ICashMovementRepository cashMovementRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterCashMovementCommand, long>
{
    public async Task<Result<long>> Handle(RegisterCashMovementCommand request, CancellationToken cancellationToken)
    {
        var session = await cashSessionRepository.GetByIdAsync(request.CashSessionId, cancellationToken);
        if (session is null || !session.IsActive)
            return Result.Failure<long>(new Error("CashSession.NotFound", "Cash session not found."));
        if (!session.IsOpen())
            return Result.Failure<long>(new Error("CashSession.NotOpen", "Cash session is not open."));

        var movement = CashMovement.Create(
            request.CashSessionId, request.CashMovementTypeId, null,
            request.EmployeeId, request.Amount, request.Description);
        if (movement.IsFailure)
            return Result.Failure<long>(movement.Error);

        await cashMovementRepository.AddAsync(movement.Value, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(movement.Value.Id);
    }
}
