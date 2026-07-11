using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Promotions.GetActive;

internal sealed class GetActivePromotionsQueryHandler(IPromotionRepository promotionRepository)
    : IQueryHandler<GetActivePromotionsQuery, IReadOnlyCollection<ActivePromotionResponse>>
{
    public async Task<Result<IReadOnlyCollection<ActivePromotionResponse>>> Handle(
        GetActivePromotionsQuery request, CancellationToken cancellationToken)
    {
        var promotions = await promotionRepository.GetByBranchAsync(request.BranchId, cancellationToken);

        // Horario LOCAL do bar (a API roda na maquina do estabelecimento).
        var localNow = DateTime.Now;

        IReadOnlyCollection<ActivePromotionResponse> response = promotions
            .Where(p => p.IsActiveAt(localNow))
            .Select(p => new ActivePromotionResponse(p.ProductId, p.Name, p.EndMinuteOfDay, p.PromotionTypeId, p.DiscountRate))
            .ToList();

        return Result.Success(response);
    }
}
