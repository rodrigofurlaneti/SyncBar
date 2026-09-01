using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Review;

internal sealed class ReplyIfoodReviewCommandHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodReviewClient reviewClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<ReplyIfoodReviewCommand, IfoodReviewReplyResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodReviewReplyResponse>> Handle(
        ReplyIfoodReviewCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ReplyIfoodReviewCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodReviewReplyResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await reviewClient.ReplyReviewAsync(token, merchantId, request.ReviewId, request.Text, cancellationToken);

                return Result.Success(new IfoodReviewReplyResponse(result.CreatedAt, result.Text, result.ReviewId));
            });
    }
}
