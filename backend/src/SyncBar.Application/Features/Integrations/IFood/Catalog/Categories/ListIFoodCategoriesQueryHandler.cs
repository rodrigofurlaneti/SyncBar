using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Categories;

internal sealed class ListIFoodCategoriesQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<ListIFoodCategoriesQuery, IReadOnlyCollection<IFoodCategoryResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IFoodCategoryResponse>>> Handle(
        ListIFoodCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ListIFoodCategoriesQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IReadOnlyCollection<IFoodCategoryResponse>>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.ListCategoriesAsync(token, merchantId, request.CatalogId, request.IncludeItems, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IReadOnlyCollection<IFoodCategoryResponse>>(new Error("IFoodCatalog.CategoriesFetchFailed", result.ErrorMessage ?? "Falha ao listar as categorias do catálogo no iFood."));

                IReadOnlyCollection<IFoodCategoryResponse> responses = result.Categories
                    .Select(c => new IFoodCategoryResponse(c.Id, c.Index, c.Name, c.ExternalCode, c.Status, c.Template))
                    .ToList();

                return Result.Success(responses);
            });
    }
}
