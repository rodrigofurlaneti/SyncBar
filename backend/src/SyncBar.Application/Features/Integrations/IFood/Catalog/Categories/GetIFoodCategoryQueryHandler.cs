using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Categories;

internal sealed class GetIfoodCategoryQueryHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodCategoryQuery, IfoodCategoryResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodCategoryResponse>> Handle(
        GetIfoodCategoryQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodCategoryQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodCategoryResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.GetCategoryAsync(token, merchantId, request.CatalogId, request.CategoryId, request.IncludeItems, cancellationToken);
                if (!result.Success || result.Category is null)
                    return Result.Failure<IfoodCategoryResponse>(new Error("IfoodCatalog.CategoryFetchFailed", result.ErrorMessage ?? "Falha ao buscar a categoria no Ifood."));

                var category = result.Category;
                return Result.Success(new IfoodCategoryResponse(
                    category.Id, category.Index, category.Name, category.ExternalCode, category.Status, category.Template));
            });
    }
}
