using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Products;

internal sealed class ListIFoodProductsQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<ListIFoodProductsQuery, IReadOnlyCollection<IFoodProductResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IFoodProductResponse>>> Handle(
        ListIFoodProductsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ListIFoodProductsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IReadOnlyCollection<IFoodProductResponse>>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.ListProductsAsync(token, merchantId, request.Limit, request.Page, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IReadOnlyCollection<IFoodProductResponse>>(new Error("IFoodCatalog.ProductsFetchFailed", result.ErrorMessage ?? "Falha ao listar os produtos no iFood."));

                IReadOnlyCollection<IFoodProductResponse> responses = result.Products
                    .Select(p => new IFoodProductResponse(p.Id, p.Name, p.Description, p.AdditionalInformation, p.ExternalCode, p.Ean, p.Industrialized, p.ImagePath))
                    .ToList();

                return Result.Success(responses);
            });
    }
}
