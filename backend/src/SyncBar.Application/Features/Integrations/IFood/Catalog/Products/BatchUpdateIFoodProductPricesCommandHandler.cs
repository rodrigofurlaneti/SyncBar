using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Products;

internal sealed class BatchUpdateIFoodProductPricesCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<BatchUpdateIFoodProductPricesCommand, IFoodBatchDispatchResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodBatchDispatchResponse>> Handle(
        BatchUpdateIFoodProductPricesCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(BatchUpdateIFoodProductPricesCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodBatchDispatchResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var items = request.Items
                    .Select(i => new IFoodBatchProductPriceItem(i.ProductId, i.ExternalCode, i.Value, i.OriginalValue, i.Resources))
                    .ToList();

                var result = await catalogClient.BatchUpdateProductPricesAsync(token, merchantId, items, request.CatalogContext, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IFoodBatchDispatchResponse>(new Error("IFoodCatalog.BatchUpdateProductPricesFailed", result.ErrorMessage ?? "Falha ao atualizar o preço dos produtos em lote no iFood."));

                return Result.Success(new IFoodBatchDispatchResponse(result.Url, result.BatchId));
            });
    }
}
