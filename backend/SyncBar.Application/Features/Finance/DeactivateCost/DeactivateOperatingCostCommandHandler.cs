using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Finance.DeactivateCost;

internal sealed class DeactivateOperatingCostCommandHandler(
    IOperatingCostRepository costRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeactivateOperatingCostCommand>
{
    public async Task<Result> Handle(DeactivateOperatingCostCommand request, CancellationToken cancellationToken)
    {
        var cost = await costRepository.GetByIdForUpdateAsync(request.OperatingCostId, cancellationToken);
        if (cost is null || !cost.IsActive)
            return Result.Failure(new Error("OperatingCost.NotFound", "Cost entry not found."));

        cost.Deactivate();
        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
