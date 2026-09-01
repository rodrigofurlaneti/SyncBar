using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Categories;

internal sealed class ListIfoodSellableItemsQueryHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<ListIfoodSellableItemsQuery, IReadOnlyCollection<IfoodSellableItemResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IfoodSellableItemResponse>>> Handle(
        ListIfoodSellableItemsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ListIfoodSellableItemsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IReadOnlyCollection<IfoodSellableItemResponse>>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.ListSellableItemsAsync(token, merchantId, request.GroupId, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IReadOnlyCollection<IfoodSellableItemResponse>>(new Error("IfoodCatalog.SellableItemsFetchFailed", result.ErrorMessage ?? "Falha ao listar os itens vendáveis no Ifood."));

                IReadOnlyCollection<IfoodSellableItemResponse> responses = result.Items
                    .Select(i => new IfoodSellableItemResponse(i.ItemId, i.CategoryId, i.ItemName, i.ItemExternalCode, i.ItemEan, i.ItemPriceValue))
                    .ToList();

                return Result.Success(responses);
            });
    }
}
