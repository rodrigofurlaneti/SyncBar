using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Items;

internal sealed class ListIFoodCategoryItemsQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<ListIFoodCategoryItemsQuery, IFoodCategoryItemsResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodCategoryItemsResponse>> Handle(
        ListIFoodCategoryItemsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ListIFoodCategoryItemsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodCategoryItemsResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.ListCategoryItemsAsync(token, merchantId, request.CategoryId, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IFoodCategoryItemsResponse>(new Error("IFoodCatalog.CategoryItemsFetchFailed", result.ErrorMessage ?? "Falha ao listar os itens da categoria no iFood."));

                return Result.Success(new IFoodCategoryItemsResponse(result.RawPayload));
            });
    }
}
