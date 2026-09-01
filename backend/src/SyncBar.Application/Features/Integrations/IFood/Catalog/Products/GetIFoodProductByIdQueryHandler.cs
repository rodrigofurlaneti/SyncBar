using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Products;

internal sealed class GetIfoodProductByIdQueryHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodProductByIdQuery, IfoodProductResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodProductResponse>> Handle(
        GetIfoodProductByIdQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodProductByIdQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodProductResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.GetProductByIdAsync(token, merchantId, request.ProductId, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IfoodProductResponse>(new Error("IfoodCatalog.ProductFetchFailed", result.ErrorMessage ?? "Falha ao buscar o produto no Ifood."));

                if (result.Product is null)
                    return Result.Failure<IfoodProductResponse>(new Error("IfoodCatalog.ProductNotFound", "Produto não encontrado no Ifood."));

                var product = result.Product;
                return Result.Success(new IfoodProductResponse(
                    product.Id, product.Name, product.Description, product.AdditionalInformation, product.ExternalCode,
                    product.Ean, product.Industrialized, product.ImagePath));
            });
    }
}
