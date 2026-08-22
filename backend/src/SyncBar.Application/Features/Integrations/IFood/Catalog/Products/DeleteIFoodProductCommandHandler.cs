using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Products;

internal sealed class DeleteIFoodProductCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<DeleteIFoodProductCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(DeleteIFoodProductCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DeleteIFoodProductCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.DeleteProductAsync(token, merchantId, request.ProductId, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IFoodCatalog.DeleteProductFailed", result.ErrorMessage ?? "Falha ao excluir o produto no iFood."));

                return Result.Success();
            });
    }
}
