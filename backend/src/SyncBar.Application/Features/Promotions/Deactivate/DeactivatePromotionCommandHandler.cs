using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Promotions.Deactivate;

internal sealed class DeactivatePromotionCommandHandler : BaseCommandHandler<DeactivatePromotionCommand>
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivatePromotionCommandHandler(
        IPromotionRepository promotionRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _promotionRepository = promotionRepository;
        _unitOfWork = unitOfWork;
    }

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

                var promotion = await _promotionRepository.GetByIdForUpdateAsync(request.PromotionId, cancellationToken);
                if (promotion is null || !promotion.IsActive)
                    return Result.Failure(new Error("Promotion.NotFound", "Promotion not found."));

                promotion.Deactivate();
                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}