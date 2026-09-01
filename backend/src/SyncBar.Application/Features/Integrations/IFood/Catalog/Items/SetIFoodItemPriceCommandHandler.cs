using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Items;

internal sealed class SetIfoodItemPriceCommandHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<SetIfoodItemPriceCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(SetIfoodItemPriceCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetIfoodItemPriceCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var priceByCatalog = request.PriceByCatalog?
                    .Select(p => new IfoodItemPriceByCatalog(p.Value, p.CatalogContext, p.OriginalValue))
                    .ToList();

                var result = await catalogClient.SetItemPriceAsync(token, merchantId, request.ItemId, request.Value, request.OriginalValue, priceByCatalog, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IfoodCatalog.SetItemPriceFailed", result.ErrorMessage ?? "Falha ao atualizar o preço do item no Ifood."));

                return Result.Success();
            });
    }
}
