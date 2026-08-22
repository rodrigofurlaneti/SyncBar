using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Admin;

internal sealed class DeleteIFoodInventoryBatchCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<DeleteIFoodInventoryBatchCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(DeleteIFoodInventoryBatchCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DeleteIFoodInventoryBatchCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.DeleteInventoryBatchAsync(token, merchantId, request.ProductIds, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IFoodCatalog.DeleteInventoryBatchFailed", result.ErrorMessage ?? "Falha ao excluir o estoque dos produtos no iFood."));

                return Result.Success();
            });
    }
}
