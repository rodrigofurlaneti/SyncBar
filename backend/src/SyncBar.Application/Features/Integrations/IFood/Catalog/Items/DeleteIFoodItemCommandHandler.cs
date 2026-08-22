using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Items;

internal sealed class DeleteIFoodItemCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<DeleteIFoodItemCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(DeleteIFoodItemCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DeleteIFoodItemCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.DeleteItemAsync(token, merchantId, request.CategoryId, request.ProductId, request.CatalogContext, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IFoodCatalog.DeleteItemFailed", result.ErrorMessage ?? "Falha ao excluir o item no iFood."));

                return Result.Success();
            });
    }
}
