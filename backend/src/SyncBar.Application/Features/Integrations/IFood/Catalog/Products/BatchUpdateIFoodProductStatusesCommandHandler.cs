using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Products;

internal sealed class BatchUpdateIFoodProductStatusesCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<BatchUpdateIFoodProductStatusesCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(BatchUpdateIFoodProductStatusesCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(BatchUpdateIFoodProductStatusesCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var items = request.Items
                    .Select(i => new IFoodBatchProductStatusItem(i.ProductId, i.ExternalCode, i.Status, i.Resources))
                    .ToList();

                var result = await catalogClient.BatchUpdateProductStatusesAsync(token, merchantId, items, request.CatalogContext, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IFoodCatalog.BatchUpdateProductStatusesFailed", result.ErrorMessage ?? "Falha ao atualizar o status dos produtos em lote no iFood."));

                return Result.Success();
            });
    }
}
