using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Categories;

internal sealed class GetIFoodCategoryQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodCategoryQuery, IFoodCategoryResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodCategoryResponse>> Handle(
        GetIFoodCategoryQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodCategoryQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodCategoryResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.GetCategoryAsync(token, merchantId, request.CatalogId, request.CategoryId, request.IncludeItems, cancellationToken);
                if (!result.Success || result.Category is null)
                    return Result.Failure<IFoodCategoryResponse>(new Error("IFoodCatalog.CategoryFetchFailed", result.ErrorMessage ?? "Falha ao buscar a categoria no iFood."));

                var category = result.Category;
                return Result.Success(new IFoodCategoryResponse(
                    category.Id, category.Index, category.Name, category.ExternalCode, category.Status, category.Template));
            });
    }
}
