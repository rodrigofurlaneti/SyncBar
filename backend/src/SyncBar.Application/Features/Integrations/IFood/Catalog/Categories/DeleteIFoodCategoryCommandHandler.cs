using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Categories;

internal sealed class DeleteIFoodCategoryCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<DeleteIFoodCategoryCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(DeleteIFoodCategoryCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DeleteIFoodCategoryCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.DeleteCategoryAsync(token, merchantId, request.CategoryId, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IFoodCatalog.DeleteCategoryFailed", result.ErrorMessage ?? "Falha ao excluir a categoria no iFood."));

                return Result.Success();
            });
    }
}
