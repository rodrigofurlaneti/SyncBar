using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.ProductExtras;

public sealed record UpdateProductBoostCommand(long ProductId, long ItemId, string BoostName, decimal IncrementalValue, int DisplayOrder) : ICommand;

internal sealed class UpdateProductBoostCommandHandler(IProductRepository products, ILogTrackerRepository logs, IUnitOfWork unitOfWork)
    : BaseCommandHandler<UpdateProductBoostCommand>(logs, unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public override Task<Result> Handle(UpdateProductBoostCommand request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(UpdateProductBoostCommandHandler), nameof(Handle), null, async _ =>
        {
            var product = await products.GetByIdForUpdateAsync(request.ProductId, ct);
            if (product is null || !product.IsActive)
                return Result.Failure(new Error("Product.NotFound", "Product not found."));
            var item = product.Boosts.FirstOrDefault(x => x.Id == request.ItemId && x.ProductId == request.ProductId && x.IsActive);
            if (item is null) return Result.Failure(new Error("ProductBoost.NotFound", "Item not found for this product."));
            var result = item.Update(request.BoostName, request.IncrementalValue, request.DisplayOrder);
            if (result.IsFailure) return result;
            product.Touch();
            await _unitOfWork.CommitAsync(ct);
            return Result.Success();
        });
}
