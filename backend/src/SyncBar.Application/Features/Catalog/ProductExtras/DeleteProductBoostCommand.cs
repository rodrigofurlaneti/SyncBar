using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.ProductExtras;

public sealed record DeleteProductBoostCommand(long ProductId, long ItemId) : ICommand;

internal sealed class DeleteProductBoostCommandHandler(IProductRepository products, ILogTrackerRepository logs, IUnitOfWork unitOfWork)
    : BaseCommandHandler<DeleteProductBoostCommand>(logs, unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public override Task<Result> Handle(DeleteProductBoostCommand request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(DeleteProductBoostCommandHandler), nameof(Handle), null, async _ =>
        {
            var product = await products.GetByIdForUpdateAsync(request.ProductId, ct);
            if (product is null || !product.IsActive)
                return Result.Failure(new Error("Product.NotFound", "Product not found."));
            var item = product.Boosts.FirstOrDefault(x => x.Id == request.ItemId && x.ProductId == request.ProductId && x.IsActive);
            if (item is null) return Result.Failure(new Error("ProductBoost.NotFound", "Item not found for this product."));
            item.Deactivate();
            product.Touch();
            await _unitOfWork.CommitAsync(ct);
            return Result.Success();
        });
}
