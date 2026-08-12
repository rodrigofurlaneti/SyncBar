using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.DeactivateProduct;

internal sealed class DeactivateProductCommandHandler : BaseCommandHandler<DeactivateProductCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateProductCommandHandler(
        IProductRepository productRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result> Handle(DeactivateProductCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(DeactivateProductCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se o IP estiver disponível no Command
            async (userIdBox) =>
            {
                var product = await _productRepository.GetByIdForUpdateAsync(request.ProductId, cancellationToken);
                if (product is null || !product.IsActive)
                    return Result.Failure(new Error("Product.NotFound", "Product not found."));

                product.Deactivate();
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success();
            });
}