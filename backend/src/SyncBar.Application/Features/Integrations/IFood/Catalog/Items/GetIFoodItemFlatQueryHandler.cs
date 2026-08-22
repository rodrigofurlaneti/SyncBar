using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Items;

internal sealed class GetIFoodItemFlatQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodItemFlatQuery, IFoodItemFlatResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodItemFlatResponse>> Handle(
        GetIFoodItemFlatQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodItemFlatQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodItemFlatResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.GetItemFlatAsync(token, merchantId, request.ItemId, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IFoodItemFlatResponse>(new Error("IFoodCatalog.ItemFlatFetchFailed", result.ErrorMessage ?? "Falha ao buscar o item no iFood."));

                return Result.Success(new IFoodItemFlatResponse(
                    result.ItemId, result.Status, result.PriceValue, result.ExternalCode, result.CategoryId, result.RawPayload));
            });
    }
}
