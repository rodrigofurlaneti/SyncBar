using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Promotions.GetByBranch;

internal sealed class GetPromotionsByBranchQueryHandler(
    IPromotionRepository promotionRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetPromotionsByBranchQuery, IReadOnlyCollection<PromotionResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<PromotionResponse>>> Handle(
        GetPromotionsByBranchQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetPromotionsByBranchQueryHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/gerente consultando a lista, preencha:

                var promotions = await promotionRepository.GetByBranchAsync(request.BranchId, cancellationToken);

                IReadOnlyCollection<PromotionResponse> response = promotions
                    .OrderBy(p => p.DayOfWeek).ThenBy(p => p.StartMinuteOfDay)
                    .Select(p => new PromotionResponse(
                        p.Id, p.ProductId, p.Name, p.DayOfWeek, p.StartMinuteOfDay, p.EndMinuteOfDay,
                        p.PromotionTypeId, p.DiscountRate))
                    .ToList();

                return Result.Success(response);
            });
    }
}