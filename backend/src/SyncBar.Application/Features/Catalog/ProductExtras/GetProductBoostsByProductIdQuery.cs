using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.ProductExtras;

public sealed record GetProductBoostsByProductIdQuery(long ProductId) : IQuery<IReadOnlyCollection<ProductBoostResponse>>;

// Management queries preserve access to active configuration while its product flag is off.
internal sealed class GetProductBoostsByProductIdQueryHandler(IProductRepository products, ILogTrackerRepository logs, IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetProductBoostsByProductIdQuery, IReadOnlyCollection<ProductBoostResponse>>(logs, unitOfWork)
{
    public override Task<Result<IReadOnlyCollection<ProductBoostResponse>>> Handle(GetProductBoostsByProductIdQuery request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetProductBoostsByProductIdQueryHandler), nameof(Handle), null, async _ =>
        {
            var product = await products.GetByIdAsync(request.ProductId, ct);
            if (product is null || !product.IsActive)
                return Result.Failure<IReadOnlyCollection<ProductBoostResponse>>(new Error("Product.NotFound", "Product not found."));
            return Result.Success<IReadOnlyCollection<ProductBoostResponse>>(product.Boosts
                .Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).Select(ProductBoostResponse.From).ToArray());
        });
}

