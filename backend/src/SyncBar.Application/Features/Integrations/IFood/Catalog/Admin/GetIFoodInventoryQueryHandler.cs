using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Admin;

internal sealed class GetIfoodInventoryQueryHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodInventoryQuery, IfoodInventoryResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodInventoryResponse>> Handle(
        GetIfoodInventoryQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodInventoryQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodInventoryResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.GetInventoryAsync(token, merchantId, request.ProductId, cancellationToken);
                if (!result.Success || result.Inventory is null)
                    return Result.Failure<IfoodInventoryResponse>(new Error("IfoodCatalog.InventoryFetchFailed", result.ErrorMessage ?? "Falha ao consultar o estoque do produto no Ifood."));

                var inventory = result.Inventory;
                return Result.Success(new IfoodInventoryResponse(
                    inventory.ProductId, inventory.OwnerId, inventory.Amount, inventory.InStock));
            });
    }
}
