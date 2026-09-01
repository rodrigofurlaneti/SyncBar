using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Review;

internal sealed class GetIfoodReviewsQueryHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodReviewClient reviewClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodReviewsQuery, IfoodReviewListResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodReviewListResponse>> Handle(
        GetIfoodReviewsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodReviewsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodReviewListResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await reviewClient.GetReviewsAsync(
                    token, merchantId, request.Page, request.PageSize, addCount: true,
                    request.DateFrom, request.DateTo, request.Sort, request.SortBy, cancellationToken);

                var items = result.Reviews
                    .Select(r => new IfoodReviewListItemResponse(
                        r.Id, r.CreatedAt, r.Discarded, r.Published, r.Comment, r.Moderated, r.ModerationStatus, r.Reply, r.Score,
                        r.Order is null ? null : new IfoodReviewOrderResponse(r.Order.CreatedAt, r.Order.Id, r.Order.ShortId)))
                    .ToList();

                return Result.Success(new IfoodReviewListResponse(result.Page, result.Size, result.Total, result.PageCount, items));
            });
    }
}
