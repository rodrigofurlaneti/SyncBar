using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.ProductExtras;

public sealed record AddOptionalExtraCommand(long ProductId, string OptionalExtraName, int DisplayOrder) : ICommand;

internal sealed class AddOptionalExtraCommandHandler(IProductRepository products, ILogTrackerRepository logs, IUnitOfWork unitOfWork)
    : BaseCommandHandler<AddOptionalExtraCommand>(logs, unitOfWork)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public override Task<Result> Handle(AddOptionalExtraCommand request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(AddOptionalExtraCommandHandler), nameof(Handle), null, async _ =>
        {
            var product = await products.GetByIdForUpdateAsync(request.ProductId, ct);
            if (product is null || !product.IsActive)
                return Result.Failure(new Error("Product.NotFound", "Product not found."));
            var created = ProductOptionalExtra.Create(product.Id, request.OptionalExtraName, request.DisplayOrder);
            if (created.IsFailure) return Result.Failure(created.Error);
            product.OptionalExtras.Add(created.Value);
            product.Touch();
            await _unitOfWork.CommitAsync(ct);
            return Result.Success();
        });
}
