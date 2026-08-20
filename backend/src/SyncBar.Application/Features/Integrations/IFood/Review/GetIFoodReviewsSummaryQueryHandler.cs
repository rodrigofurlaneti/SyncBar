using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Review;

internal sealed class GetIFoodReviewsSummaryQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodReviewClient reviewClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodReviewsSummaryQuery, IFoodReviewSummaryResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodReviewSummaryResponse>> Handle(
        GetIFoodReviewsSummaryQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodReviewsSummaryQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodReviewSummaryResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var summary = await reviewClient.GetSummaryAsync(token, merchantId, cancellationToken);
                if (summary is null)
                    return Result.Failure<IFoodReviewSummaryResponse>(new Error("IFoodReview.SummaryFailed", "Failed to fetch review summary from iFood."));

                return Result.Success(new IFoodReviewSummaryResponse(summary.Score, summary.TotalReviewsCount, summary.ValidReviewsCount));
            });
    }
}
