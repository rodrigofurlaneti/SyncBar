using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Storage;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.SetProductImage;

internal sealed class SetProductImageCommandHandler(
    IProductRepository productRepository,
    IImageStorage imageStorage,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetProductImageCommand, string>
{
    public async Task<Result<string>> Handle(SetProductImageCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdForUpdateAsync(request.ProductId, cancellationToken);
        if (product is null || !product.IsActive)
            return Result.Failure<string>(new Error("Product.NotFound", "Product not found."));

        var url = await imageStorage.SaveProductImageAsync(
            product.Id, request.Extension.ToLowerInvariant(), request.Content, cancellationToken);

        product.SetImage(url);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(url);
    }
}
