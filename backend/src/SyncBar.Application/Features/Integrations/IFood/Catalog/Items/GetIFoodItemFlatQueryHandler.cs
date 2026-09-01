using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Items;

internal sealed class GetIfoodItemFlatQueryHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodItemFlatQuery, IfoodItemFlatResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodItemFlatResponse>> Handle(
        GetIfoodItemFlatQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodItemFlatQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodItemFlatResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.GetItemFlatAsync(token, merchantId, request.ItemId, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IfoodItemFlatResponse>(new Error("IfoodCatalog.ItemFlatFetchFailed", result.ErrorMessage ?? "Falha ao buscar o item no Ifood."));

                return Result.Success(new IfoodItemFlatResponse(
                    result.ItemId, result.Status, result.PriceValue, result.ExternalCode, result.CategoryId, result.RawPayload));
            });
    }
}
