using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Categories;

internal sealed class ListIFoodSellableItemsQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<ListIFoodSellableItemsQuery, IReadOnlyCollection<IFoodSellableItemResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IFoodSellableItemResponse>>> Handle(
        ListIFoodSellableItemsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ListIFoodSellableItemsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IReadOnlyCollection<IFoodSellableItemResponse>>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.ListSellableItemsAsync(token, merchantId, request.GroupId, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IReadOnlyCollection<IFoodSellableItemResponse>>(new Error("IFoodCatalog.SellableItemsFetchFailed", result.ErrorMessage ?? "Falha ao listar os itens vendáveis no iFood."));

                IReadOnlyCollection<IFoodSellableItemResponse> responses = result.Items
                    .Select(i => new IFoodSellableItemResponse(i.ItemId, i.CategoryId, i.ItemName, i.ItemExternalCode, i.ItemEan, i.ItemPriceValue))
                    .ToList();

                return Result.Success(responses);
            });
    }
}
