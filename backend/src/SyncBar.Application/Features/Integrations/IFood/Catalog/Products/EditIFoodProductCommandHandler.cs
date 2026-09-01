using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Products;

internal sealed class EditIfoodProductCommandHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<EditIfoodProductCommand, IfoodProductResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodProductResponse>> Handle(
        EditIfoodProductCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(EditIfoodProductCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodProductResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                // O Id do produto já vai na rota (productId) — não duplicado no corpo.
                var upsertRequest = new IfoodUpsertProductRequest(
                    null, request.Name, request.Description, request.AdditionalInformation, request.ExternalCode,
                    request.Ean, request.Image,
                    request.Shifts?.Select(s => new IfoodProductShift(
                        s.StartTime, s.EndTime, s.Monday, s.Tuesday, s.Wednesday, s.Thursday, s.Friday, s.Saturday, s.Sunday)).ToList());

                var result = await catalogClient.EditProductAsync(token, merchantId, request.ProductId, upsertRequest, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IfoodProductResponse>(new Error("IfoodCatalog.EditProductFailed", result.ErrorMessage ?? "Falha ao editar o produto no Ifood."));

                var product = result.Product;
                return Result.Success(new IfoodProductResponse(
                    product?.Id, product?.Name, product?.Description, product?.AdditionalInformation, product?.ExternalCode,
                    product?.Ean, product?.Industrialized, product?.ImagePath));
            });
    }
}
