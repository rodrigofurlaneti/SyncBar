using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.ProductExtras;

public sealed record UpdateOptionalExtraCommand(long ProductId, long ItemId, string OptionalExtraName, int DisplayOrder) : ICommand;

internal sealed class UpdateOptionalExtraCommandHandler(IProductRepository products, ILogTrackerRepository logs, IUnitOfWork unitOfWork)
    : BaseCommandHandler<UpdateOptionalExtraCommand>(logs, unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public override Task<Result> Handle(UpdateOptionalExtraCommand request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(UpdateOptionalExtraCommandHandler), nameof(Handle), null, async _ =>
        {
            var product = await products.GetByIdForUpdateAsync(request.ProductId, ct);
            if (product is null || !product.IsActive)
                return Result.Failure(new Error("Product.NotFound", "Product not found."));
            var item = product.OptionalExtras.FirstOrDefault(x => x.Id == request.ItemId && x.ProductId == request.ProductId && x.IsActive);
            if (item is null) return Result.Failure(new Error("ProductOptionalExtra.NotFound", "Item not found for this product."));
            var result = item.Update(request.OptionalExtraName, request.DisplayOrder);
            if (result.IsFailure) return result;
            product.Touch();
            await _unitOfWork.CommitAsync(ct);
            return Result.Success();
        });
}
