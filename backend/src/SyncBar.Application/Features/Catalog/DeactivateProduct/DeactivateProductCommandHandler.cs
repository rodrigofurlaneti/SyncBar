using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.DeactivateProduct;

internal sealed class DeactivateProductCommandHandler(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeactivateProductCommand>
{
    public async Task<Result> Handle(DeactivateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdForUpdateAsync(request.ProductId, cancellationToken);
        if (product is null || !product.IsActive)
            return Result.Failure(new Error("Product.NotFound", "Product not found."));

        product.Deactivate();
        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
