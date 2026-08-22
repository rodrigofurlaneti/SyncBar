using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Products;

internal sealed class GetIFoodProductByIdQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodProductByIdQuery, IFoodProductResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodProductResponse>> Handle(
        GetIFoodProductByIdQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodProductByIdQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodProductResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.GetProductByIdAsync(token, merchantId, request.ProductId, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IFoodProductResponse>(new Error("IFoodCatalog.ProductFetchFailed", result.ErrorMessage ?? "Falha ao buscar o produto no iFood."));

                if (result.Product is null)
                    return Result.Failure<IFoodProductResponse>(new Error("IFoodCatalog.ProductNotFound", "Produto não encontrado no iFood."));

                var product = result.Product;
                return Result.Success(new IFoodProductResponse(
                    product.Id, product.Name, product.Description, product.AdditionalInformation, product.ExternalCode,
                    product.Ean, product.Industrialized, product.ImagePath));
            });
    }
}
