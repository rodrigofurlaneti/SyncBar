using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.DeactivateProduct;

internal sealed class DeactivateProductCommandHandler : BaseCommandHandler<DeactivateProductCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateProductCommandHandler(
        IProductRepository productRepository,
        IIfoodCatalogSyncTrigger catalogSyncTrigger,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _productRepository = productRepository;
        _catalogSyncTrigger = catalogSyncTrigger;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result> Handle(DeactivateProductCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(DeactivateProductCommandHandler),
            nameof(Handle),
            null, 
            async (userIdBox) =>
            {
                var product = await _productRepository.GetByIdForUpdateAsync(request.ProductId, cancellationToken);
                if (product is null || !product.IsActive)
                    return Result.Failure(new Error("Product.NotFound", "Product not found."));
                product.Deactivate();
                await _unitOfWork.CommitAsync(cancellationToken);
                _catalogSyncTrigger.TriggerCompanySync(product.CompanyId);
                return Result.Success();
            });
}