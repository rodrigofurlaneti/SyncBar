using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Review;

internal sealed class GetIFoodReviewByIdQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodReviewClient reviewClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodReviewByIdQuery, IFoodReviewDetailResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodReviewDetailResponse>> Handle(
        GetIFoodReviewByIdQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodReviewByIdQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodReviewDetailResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var review = await reviewClient.GetReviewByIdAsync(token, merchantId, request.ReviewId, cancellationToken);
                if (review is null)
                    return Result.Failure<IFoodReviewDetailResponse>(new Error("IFoodReview.NotFound", "Review not found on iFood."));

                var questions = review.Questions
                    .Select(q => new IFoodReviewQuestionResponse(
                        q.Id, q.Type, q.Title, q.Answers.Select(a => new IFoodReviewAnswerOptionResponse(a.Id, a.Title)).ToList()))
                    .ToList();

                var response = new IFoodReviewDetailResponse(
                    review.Id, review.CreatedAt, review.Discarded, review.Published, review.Comment, review.CustomerName,
                    review.Moderated, review.ModerationStatus, review.Reply, review.Score,
                    review.Order is null ? null : new IFoodReviewOrderResponse(review.Order.CreatedAt, review.Order.Id, review.Order.ShortId),
                    questions);

                return Result.Success(response);
            });
    }
}
