using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Review;

internal sealed class GetIfoodReviewByIdQueryHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodReviewClient reviewClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodReviewByIdQuery, IfoodReviewDetailResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodReviewDetailResponse>> Handle(
        GetIfoodReviewByIdQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodReviewByIdQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodReviewDetailResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var review = await reviewClient.GetReviewByIdAsync(token, merchantId, request.ReviewId, cancellationToken);
                if (review is null)
                    return Result.Failure<IfoodReviewDetailResponse>(new Error("IfoodReview.NotFound", "Review not found on Ifood."));

                var questions = review.Questions
                    .Select(q => new IfoodReviewQuestionResponse(
                        q.Id, q.Type, q.Title, q.Answers.Select(a => new IfoodReviewAnswerOptionResponse(a.Id, a.Title)).ToList()))
                    .ToList();

                var response = new IfoodReviewDetailResponse(
                    review.Id, review.CreatedAt, review.Discarded, review.Published, review.Comment, review.CustomerName,
                    review.Moderated, review.ModerationStatus, review.Reply, review.Score,
                    review.Order is null ? null : new IfoodReviewOrderResponse(review.Order.CreatedAt, review.Order.Id, review.Order.ShortId),
                    questions);

                return Result.Success(response);
            });
    }
}
