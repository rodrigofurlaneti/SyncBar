using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Complements.LinkProductComplementGroup;

internal sealed class LinkProductComplementGroupCommandHandler : BaseCommandHandler<LinkProductComplementGroupCommand, long>
{
    private readonly IProductRepository _productRepository;
    private readonly IComplementGroupRepository _complementGroupRepository;
    private readonly IProductComplementGroupRepository _productComplementGroupRepository;
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger;
    private readonly IUnitOfWork _unitOfWork;

    public LinkProductComplementGroupCommandHandler(
        IProductRepository productRepository,
        IComplementGroupRepository complementGroupRepository,
        IProductComplementGroupRepository productComplementGroupRepository,
        IIfoodCatalogSyncTrigger catalogSyncTrigger,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _productRepository = productRepository;
        _complementGroupRepository = complementGroupRepository;
        _productComplementGroupRepository = productComplementGroupRepository;
        _catalogSyncTrigger = catalogSyncTrigger;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result<long>> Handle(LinkProductComplementGroupCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(LinkProductComplementGroupCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se aplicável
            async (userIdBox) =>
            {
                var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
                if (product is null || !product.IsActive)
                    return Result.Failure<long>(new Error("Product.NotFound", "Product not found."));

                var complementGroup = await _complementGroupRepository.GetByIdAsync(request.ComplementGroupId, cancellationToken);
                if (complementGroup is null || !complementGroup.IsActive || complementGroup.CompanyId != product.CompanyId)
                    return Result.Failure<long>(new Error("ComplementGroup.NotFound", "Complement group not found for this company."));

                var existingLinks = await _productComplementGroupRepository.GetByProductForUpdateAsync(request.ProductId, cancellationToken);
                if (existingLinks.Any(l => l.IsActive && l.ComplementGroupId == request.ComplementGroupId))
                    return Result.Failure<long>(new Error("ProductComplementGroup.AlreadyLinked", "This complement group is already linked to the product."));

                var link = ProductComplementGroup.Create(request.ProductId, request.ComplementGroupId, request.DisplayOrder);
                if (link.IsFailure)
                    return Result.Failure<long>(link.Error);

                await _productComplementGroupRepository.AddAsync(link.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                _catalogSyncTrigger.TriggerCompanySync(product.CompanyId);

                return Result.Success(link.Value.Id);
            });
}
