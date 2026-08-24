using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Promotions.GetActive;

internal sealed class GetActivePromotionsQueryHandler(
    IPromotionRepository promotionRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetActivePromotionsQuery, IReadOnlyCollection<ActivePromotionResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<ActivePromotionResponse>>> Handle(
        GetActivePromotionsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetActivePromotionsQueryHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/sistema consultando, preencha:

                var promotions = await promotionRepository.GetByBranchAsync(request.BranchId, cancellationToken);

                // Horario LOCAL do bar (a API roda na maquina do estabelecimento).
                var localNow = DateTime.Now;

                IReadOnlyCollection<ActivePromotionResponse> response = promotions
                    .Where(p => p.IsActiveAt(localNow))
                    .Select(p => new ActivePromotionResponse(p.ProductId, p.Name, p.EndMinuteOfDay, p.PromotionTypeId, p.DiscountRate))
                    .ToList();

                return Result.Success(response);
            });
    }
}