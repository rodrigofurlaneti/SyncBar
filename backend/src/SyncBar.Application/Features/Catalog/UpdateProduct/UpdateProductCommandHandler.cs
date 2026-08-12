using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.UpdateProduct;

internal sealed class UpdateProductCommandHandler : BaseCommandHandler<UpdateProductCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(UpdateProductCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se o IP estiver disponível no Command
            async (userIdBox) =>
            {
                var product = await _productRepository.GetByIdForUpdateAsync(request.ProductId, cancellationToken);
                if (product is null || !product.IsActive)
                    return Result.Failure(new Error("Product.NotFound", "Product not found."));

                var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
                if (category is null || !category.IsActive || category.CompanyId != product.CompanyId)
                    return Result.Failure(new Error("Category.NotFound", "Category not found for this company."));

                // Nota: itens ja lancados nao mudam — UnitPrice foi congelado no lancamento.
                var result = product.UpdateDetails(
                    request.CategoryId, request.UnitOfMeasureId, request.Name, request.Description,
                    request.Barcode, request.SalePrice, request.CostPrice, request.IsStockControlled,
                    request.PreparationTimeMinutes);

                if (result.IsFailure)
                    return result;

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
}