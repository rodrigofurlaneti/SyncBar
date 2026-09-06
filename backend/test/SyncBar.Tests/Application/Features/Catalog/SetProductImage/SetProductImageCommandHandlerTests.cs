using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Storage;
using SyncBar.Application.Features.Catalog.SetProductImage;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.SetProductImage;

public sealed class SetProductImageCommandHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IImageStorage _imageStorage = Substitute.For<IImageStorage>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SetProductImageCommandHandler _handler;

    public SetProductImageCommandHandlerTests()
    {
        _handler = new SetProductImageCommandHandler(_productRepository, _imageStorage, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static Product CreateProduct(long id = 1, bool active = true)
    {
        var product = Product.Create(1, 1, 1, "X-Burguer", null, null, 25m, 10m, false, null).Value;
        if (!active)
            product.Deactivate();
        SetId(product, id);
        return product;
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnFailure()
    {
        _productRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _handler.Handle(new SetProductImageCommand(1, ".png", [1, 2, 3]), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    [Fact]
    public async Task Handle_ProductInactive_ShouldReturnFailure()
    {
        var product = CreateProduct(active: false);
        _productRepository.GetByIdForUpdateAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(new SetProductImageCommand(product.Id, ".png", [1, 2, 3]), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldSaveImageAndSetUrl()
    {
        var product = CreateProduct();
        _productRepository.GetByIdForUpdateAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _imageStorage.SaveProductImageAsync(product.Id, ".png", Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns("/images/products/1.png");

        var result = await _handler.Handle(new SetProductImageCommand(product.Id, ".PNG", [1, 2, 3]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/images/products/1.png");
        product.ImageUrl.Should().Be("/images/products/1.png");
        await _imageStorage.Received(1).SaveProductImageAsync(product.Id, ".png", Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }
}
