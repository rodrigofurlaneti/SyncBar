using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Storage;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.SetProductImage;

internal sealed class SetProductImageCommandHandler(
    IProductRepository productRepository,
    IImageStorage imageStorage,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<SetProductImageCommand, string>(logRepository, unitOfWork)
{
    public override Task<Result<string>> Handle(SetProductImageCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(SetProductImageCommandHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se o IP estiver disponível no Command
            async (userIdBox) =>
            {
                var product = await productRepository.GetByIdForUpdateAsync(request.ProductId, cancellationToken);
                if (product is null || !product.IsActive)
                    return Result.Failure<string>(new Error("Product.NotFound", "Product not found."));

                var url = await imageStorage.SaveProductImageAsync(
                    product.Id, request.Extension.ToLowerInvariant(), request.Content, cancellationToken);

                product.SetImage(url);
                await unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(url);
            });
}