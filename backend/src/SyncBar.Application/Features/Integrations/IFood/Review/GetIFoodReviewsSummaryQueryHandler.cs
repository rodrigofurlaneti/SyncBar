using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Review;

internal sealed class GetIfoodReviewsSummaryQueryHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodReviewClient reviewClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodReviewsSummaryQuery, IfoodReviewSummaryResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodReviewSummaryResponse>> Handle(
        GetIfoodReviewsSummaryQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodReviewsSummaryQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodReviewSummaryResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var summary = await reviewClient.GetSummaryAsync(token, merchantId, cancellationToken);
                if (summary is null)
                    return Result.Failure<IfoodReviewSummaryResponse>(new Error("IfoodReview.SummaryFailed", "Failed to fetch review summary from Ifood."));

                return Result.Success(new IfoodReviewSummaryResponse(summary.Score, summary.TotalReviewsCount, summary.ValidReviewsCount));
            });
    }
}
