using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

internal sealed class DeleteIfoodInterruptionCommandHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodMerchantClient merchantClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<DeleteIfoodInterruptionCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(DeleteIfoodInterruptionCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DeleteIfoodInterruptionCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await merchantClient.DeleteInterruptionAsync(token, merchantId, request.InterruptionId, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IfoodMerchant.DeleteInterruptionFailed", result.ErrorMessage ?? "Failed to reopen the store on Ifood."));

                return Result.Success();
            });
    }
}
