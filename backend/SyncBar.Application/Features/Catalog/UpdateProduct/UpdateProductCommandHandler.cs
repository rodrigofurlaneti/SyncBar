using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.UpdateProduct;

internal sealed class UpdateProductCommandHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateProductCommand>
{
    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdForUpdateAsync(request.ProductId, cancellationToken);
        if (product is null || !product.IsActive)
            return Result.Failure(new Error("Product.NotFound", "Product not found."));

        var category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive || category.CompanyId != product.CompanyId)
            return Result.Failure(new Error("Category.NotFound", "Category not found for this company."));

        // Nota: itens ja lancados nao mudam — UnitPrice foi congelado no lancamento.
        var result = product.UpdateDetails(
            request.CategoryId, request.UnitOfMeasureId, request.Name, request.Description,
            request.Barcode, request.SalePrice, request.CostPrice, request.IsStockControlled,
            request.PreparationTimeMinutes);
        if (result.IsFailure)
            return result;

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
