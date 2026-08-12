using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Finance.DeactivateCost;

internal sealed class DeactivateOperatingCostCommandHandler(
    IOperatingCostRepository costRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<DeactivateOperatingCostCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(DeactivateOperatingCostCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DeactivateOperatingCostCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário que está executando a ação, preencha:
                // userIdBox.Value = request.UserId;

                var cost = await costRepository.GetByIdForUpdateAsync(request.OperatingCostId, cancellationToken);
                if (cost is null || !cost.IsActive)
                    return Result.Failure(new Error("OperatingCost.NotFound", "Cost entry not found."));

                cost.Deactivate();
                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}