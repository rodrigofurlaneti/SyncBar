using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Promotions.Deactivate;

internal sealed class DeactivatePromotionCommandHandler(
    IPromotionRepository promotionRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<DeactivatePromotionCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(DeactivatePromotionCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DeactivatePromotionCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/gerente responsável pela desativação, preencha:
                // userIdBox.Value = request.UserId;

                var promotion = await promotionRepository.GetByIdForUpdateAsync(request.PromotionId, cancellationToken);
                if (promotion is null || !promotion.IsActive)
                    return Result.Failure(new Error("Promotion.NotFound", "Promotion not found."));

                promotion.Deactivate();
                await unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}