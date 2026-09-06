using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Catalog.GetProductById;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.GetProductById;

public sealed class GetProductByIdQueryHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetProductByIdQueryHandler _handler;

    public GetProductByIdQueryHandlerTests()
    {
        _handler = new GetProductByIdQueryHandler(_productRepository, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnFailure()
    {
        _productRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _handler.Handle(new GetProductByIdQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    [Fact]
    public async Task Handle_ProductInactive_ShouldReturnFailure()
    {
        var product = Product.Create(1, 1, 1, "X-Burguer", null, null, 25m, null, false, null).Value;
        product.Deactivate();
        SetId(product, 1);
        _productRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(new GetProductByIdQuery(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    [Fact]
    public async Task Handle_ValidProduct_ShouldReturnResponse()
    {
        var product = Product.Create(1, 2, 3, "X-Burguer", "Descrição", "789123", 25m, 10m, true, 15).Value;
        product.SetImage("/images/products/1.png");
        SetId(product, 1);
        _productRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(new GetProductByIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(1);
        result.Value.CategoryId.Should().Be(2);
        result.Value.UnitOfMeasureId.Should().Be(3);
        result.Value.Name.Should().Be("X-Burguer");
        result.Value.Description.Should().Be("Descrição");
        result.Value.Barcode.Should().Be("789123");
        result.Value.SalePrice.Should().Be(25m);
        result.Value.CostPrice.Should().Be(10m);
        result.Value.IsStockControlled.Should().BeTrue();
        result.Value.PreparationTimeMinutes.Should().Be(15);
        result.Value.ImageUrl.Should().Be("/images/products/1.png");
    }
}
