using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Catalog.GetProductById
{
    internal sealed class GetProductByIdQueryHandler(
        IProductRepository productRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : BaseQueryHandler<GetProductByIdQuery, ProductResponse>(logRepository, unitOfWork)
    {
        public override async Task<Result<ProductResponse>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetProductByIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken);
                    if (product is null || !product.IsActive)
                        return Result.Failure<ProductResponse>(new Error("Product.NotFound", "Product not found."));
                    var response = new ProductResponse(
                        product.Id,
                        product.CategoryId,
                        product.UnitOfMeasureId,
                        product.Name,
                        product.Description,
                        product.Barcode,
                        product.SalePrice,
                        product.CostPrice,
                        product.IsStockControlled,
                        product.PreparationTimeMinutes,
                        product.ImageUrl
                    );
                    return Result.Success(response);
                });
        }
    }
}
