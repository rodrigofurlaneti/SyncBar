using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.ProductExtras;

public sealed record GetOptionalExtrasByProductIdQuery(long ProductId) : IQuery<IReadOnlyCollection<ProductOptionalExtraResponse>>;

// Management queries preserve access to active configuration while its product flag is off.
internal sealed class GetOptionalExtrasByProductIdQueryHandler(IProductRepository products, ILogTrackerRepository logs, IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetOptionalExtrasByProductIdQuery, IReadOnlyCollection<ProductOptionalExtraResponse>>(logs, unitOfWork)
{
    public override Task<Result<IReadOnlyCollection<ProductOptionalExtraResponse>>> Handle(GetOptionalExtrasByProductIdQuery request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetOptionalExtrasByProductIdQueryHandler), nameof(Handle), null, async _ =>
        {
            var product = await products.GetByIdAsync(request.ProductId, ct);
            if (product is null || !product.IsActive)
                return Result.Failure<IReadOnlyCollection<ProductOptionalExtraResponse>>(new Error("Product.NotFound", "Product not found."));
            return Result.Success<IReadOnlyCollection<ProductOptionalExtraResponse>>(product.OptionalExtras
                .Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).Select(ProductOptionalExtraResponse.From).ToArray());
        });
}

