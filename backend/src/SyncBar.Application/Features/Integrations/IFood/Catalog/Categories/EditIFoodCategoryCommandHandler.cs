using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Categories;

internal sealed class EditIFoodCategoryCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<EditIFoodCategoryCommand, IFoodCategoryResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodCategoryResponse>> Handle(
        EditIFoodCategoryCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(EditIFoodCategoryCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodCategoryResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.EditCategoryAsync(
                    token, merchantId, request.CatalogId, request.CategoryId, request.Name, request.ExternalCode, request.Status, request.Index, cancellationToken);
                if (!result.Success || result.Category is null)
                    return Result.Failure<IFoodCategoryResponse>(new Error("IFoodCatalog.EditCategoryFailed", result.ErrorMessage ?? "Falha ao editar a categoria no iFood."));

                var category = result.Category;
                return Result.Success(new IFoodCategoryResponse(
                    category.Id, category.Index, category.Name, category.ExternalCode, category.Status, category.Template));
            });
    }
}
