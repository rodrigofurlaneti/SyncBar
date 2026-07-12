using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Storage;
using SyncBar.Application.Features.Catalog.SetProductImage;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application;

public sealed class SetProductImageCommandHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IImageStorage _imageStorage = Substitute.For<IImageStorage>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_ShouldStoreImageAndSetUrl()
    {
        var product = Product.Create(1, 1, 1, "Cerveja", null, null, 10m, 5m, true, null).Value;
        _productRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(product);
        _imageStorage.SaveProductImageAsync(Arg.Any<long>(), ".jpg", Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns("/uploads/products/1.jpg?v=1");

        var handler = new SetProductImageCommandHandler(_productRepository, _imageStorage, _unitOfWork);
        var result = await handler.Handle(new SetProductImageCommand(1, ".JPG", [1, 2, 3]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/uploads/products/1.jpg?v=1");
        product.ImageUrl.Should().Be("/uploads/products/1.jpg?v=1");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownProduct_ShouldFail()
    {
        _productRepository.GetByIdForUpdateAsync(9, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var handler = new SetProductImageCommandHandler(_productRepository, _imageStorage, _unitOfWork);
        var result = await handler.Handle(new SetProductImageCommand(9, ".png", [1]), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }
}
