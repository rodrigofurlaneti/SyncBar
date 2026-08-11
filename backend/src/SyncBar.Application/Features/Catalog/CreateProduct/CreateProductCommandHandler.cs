using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.CreateProduct;

internal sealed class CreateProductCommandHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<CreateProductCommand, long>(logRepository, unitOfWork)
{
    public override Task<Result<long>> Handle(CreateProductCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(CreateProductCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se o IP estiver disponível no Command
            async (userIdBox) =>
            {
                var category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
                if (category is null || !category.IsActive || category.CompanyId != request.CompanyId)
                    return Result.Failure<long>(new Error("Category.NotFound", "Category not found for this company."));

                var product = Product.Create(
                    request.CompanyId, request.CategoryId, request.UnitOfMeasureId, request.Name,
                    request.Description, request.Barcode, request.SalePrice, request.CostPrice,
                    request.IsStockControlled, request.PreparationTimeMinutes);

                if (product.IsFailure)
                    return Result.Failure<long>(product.Error);

                await productRepository.AddAsync(product.Value, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(product.Value.Id);
            });
}