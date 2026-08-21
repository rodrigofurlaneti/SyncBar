using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Review;

internal sealed class GetIFoodReviewsQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodReviewClient reviewClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodReviewsQuery, IFoodReviewListResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodReviewListResponse>> Handle(
        GetIFoodReviewsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodReviewsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodReviewListResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await reviewClient.GetReviewsAsync(
                    token, merchantId, request.Page, request.PageSize, addCount: true,
                    request.DateFrom, request.DateTo, request.Sort, request.SortBy, cancellationToken);

                var items = result.Reviews
                    .Select(r => new IFoodReviewListItemResponse(
                        r.Id, r.CreatedAt, r.Discarded, r.Published, r.Comment, r.Moderated, r.ModerationStatus, r.Reply, r.Score,
                        r.Order is null ? null : new IFoodReviewOrderResponse(r.Order.CreatedAt, r.Order.Id, r.Order.ShortId)))
                    .ToList();

                return Result.Success(new IFoodReviewListResponse(result.Page, result.Size, result.Total, result.PageCount, items));
            });
    }
}
