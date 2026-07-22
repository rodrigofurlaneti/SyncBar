using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Promotions.Deactivate;

internal sealed class DeactivatePromotionCommandHandler(
    IPromotionRepository promotionRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeactivatePromotionCommand>
{
    public async Task<Result> Handle(DeactivatePromotionCommand request, CancellationToken cancellationToken)
    {
        var promotion = await promotionRepository.GetByIdForUpdateAsync(request.PromotionId, cancellationToken);
        if (promotion is null || !promotion.IsActive)
            return Result.Failure(new Error("Promotion.NotFound", "Promotion not found."));

        promotion.Deactivate();
        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
