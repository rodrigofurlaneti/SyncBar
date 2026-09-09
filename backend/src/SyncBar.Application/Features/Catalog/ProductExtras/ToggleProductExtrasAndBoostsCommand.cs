using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.ProductExtras;

public sealed record ToggleProductExtrasAndBoostsCommand(long ProductId, bool HasOptionalExtras, bool HasBoosts) : ICommand;
internal sealed class ToggleProductExtrasAndBoostsCommandHandler(IProductRepository products, ILogTrackerRepository logs, IUnitOfWork unitOfWork)
    : BaseCommandHandler<ToggleProductExtrasAndBoostsCommand>(logs, unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public override Task<Result> Handle(ToggleProductExtrasAndBoostsCommand request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(ToggleProductExtrasAndBoostsCommandHandler), nameof(Handle), null, async _ =>
        {
            var product = await products.GetByIdForUpdateAsync(request.ProductId, ct);
            if (product is null || !product.IsActive)
                return Result.Failure(new Error("Product.NotFound", "Product not found."));
            product.ToggleExtrasAndBoosts(request.HasOptionalExtras, request.HasBoosts);
            await _unitOfWork.CommitAsync(ct);
            return Result.Success();
        });
}
