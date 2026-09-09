using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.ProductExtras;

public sealed record AddProductBoostCommand(long ProductId, string BoostName, decimal IncrementalValue, int DisplayOrder) : ICommand;

internal sealed class AddProductBoostCommandHandler(IProductRepository products, ILogTrackerRepository logs, IUnitOfWork unitOfWork)
    : BaseCommandHandler<AddProductBoostCommand>(logs, unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public override Task<Result> Handle(AddProductBoostCommand request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(AddProductBoostCommandHandler), nameof(Handle), null, async _ =>
        {
            var product = await products.GetByIdForUpdateAsync(request.ProductId, ct);
            if (product is null || !product.IsActive)
                return Result.Failure(new Error("Product.NotFound", "Product not found."));
            var created = ProductBoost.Create(product.Id, request.BoostName, request.IncrementalValue, request.DisplayOrder);
            if (created.IsFailure) return Result.Failure(created.Error);
            product.Boosts.Add(created.Value);
            product.Touch();
            await _unitOfWork.CommitAsync(ct);
            return Result.Success();
        });
}
