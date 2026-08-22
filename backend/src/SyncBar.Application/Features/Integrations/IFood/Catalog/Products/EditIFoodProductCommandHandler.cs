using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Products;

internal sealed class EditIFoodProductCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<EditIFoodProductCommand, IFoodProductResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodProductResponse>> Handle(
        EditIFoodProductCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(EditIFoodProductCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodProductResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                // O Id do produto já vai na rota (productId) — não duplicado no corpo.
                var upsertRequest = new IFoodUpsertProductRequest(
                    null, request.Name, request.Description, request.AdditionalInformation, request.ExternalCode,
                    request.Ean, request.Image,
                    request.Shifts?.Select(s => new IFoodProductShift(
                        s.StartTime, s.EndTime, s.Monday, s.Tuesday, s.Wednesday, s.Thursday, s.Friday, s.Saturday, s.Sunday)).ToList());

                var result = await catalogClient.EditProductAsync(token, merchantId, request.ProductId, upsertRequest, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IFoodProductResponse>(new Error("IFoodCatalog.EditProductFailed", result.ErrorMessage ?? "Falha ao editar o produto no iFood."));

                var product = result.Product;
                return Result.Success(new IFoodProductResponse(
                    product?.Id, product?.Name, product?.Description, product?.AdditionalInformation, product?.ExternalCode,
                    product?.Ean, product?.Industrialized, product?.ImagePath));
            });
    }
}
