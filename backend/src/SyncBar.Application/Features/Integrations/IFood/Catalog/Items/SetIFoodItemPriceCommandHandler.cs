using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Items;

internal sealed class SetIFoodItemPriceCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<SetIFoodItemPriceCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(SetIFoodItemPriceCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetIFoodItemPriceCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var priceByCatalog = request.PriceByCatalog?
                    .Select(p => new IFoodItemPriceByCatalog(p.Value, p.CatalogContext, p.OriginalValue))
                    .ToList();

                var result = await catalogClient.SetItemPriceAsync(token, merchantId, request.ItemId, request.Value, request.OriginalValue, priceByCatalog, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IFoodCatalog.SetItemPriceFailed", result.ErrorMessage ?? "Falha ao atualizar o preço do item no iFood."));

                return Result.Success();
            });
    }
}
