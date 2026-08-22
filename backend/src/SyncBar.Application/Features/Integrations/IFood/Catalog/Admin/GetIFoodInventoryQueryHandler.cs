using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Admin;

internal sealed class GetIFoodInventoryQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodInventoryQuery, IFoodInventoryResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodInventoryResponse>> Handle(
        GetIFoodInventoryQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodInventoryQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodInventoryResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.GetInventoryAsync(token, merchantId, request.ProductId, cancellationToken);
                if (!result.Success || result.Inventory is null)
                    return Result.Failure<IFoodInventoryResponse>(new Error("IFoodCatalog.InventoryFetchFailed", result.ErrorMessage ?? "Falha ao consultar o estoque do produto no iFood."));

                var inventory = result.Inventory;
                return Result.Success(new IFoodInventoryResponse(
                    inventory.ProductId, inventory.OwnerId, inventory.Amount, inventory.InStock));
            });
    }
}
