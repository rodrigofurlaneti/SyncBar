using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Review;

internal sealed class ReplyIFoodReviewCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodReviewClient reviewClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<ReplyIFoodReviewCommand, IFoodReviewReplyResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodReviewReplyResponse>> Handle(
        ReplyIFoodReviewCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ReplyIFoodReviewCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodReviewReplyResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await reviewClient.ReplyReviewAsync(token, merchantId, request.ReviewId, request.Text, cancellationToken);

                return Result.Success(new IFoodReviewReplyResponse(result.CreatedAt, result.Text, result.ReviewId));
            });
    }
}
