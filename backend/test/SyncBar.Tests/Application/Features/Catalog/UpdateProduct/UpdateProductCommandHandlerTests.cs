using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.UpdateProduct;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.UpdateProduct;

public sealed class UpdateProductCommandHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger = Substitute.For<IIfoodCatalogSyncTrigger>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateProductCommandHandler _handler;

    public UpdateProductCommandHandlerTests()
    {
        _handler = new UpdateProductCommandHandler(
            _productRepository, _categoryRepository, _catalogSyncTrigger, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static Product CreateProduct(long id = 1, long companyId = 4, bool active = true)
    {
        var product = Product.Create(companyId, 1, 1, "X-Burguer", null, null, 25m, 10m, false, null).Value;
        if (!active)
            product.Deactivate();
        SetId(product, id);
        return product;
    }

    private static Category CreateCategory(long id, long companyId, bool active = true)
    {
        var category = Category.Create(companyId, "Bebidas", 1).Value;
        if (!active)
            category.Deactivate();
        SetId(category, id);
        return category;
    }

    private static UpdateProductCommand ValidCommand(long productId, long categoryId) =>
        new(productId, categoryId, 1, "X-Burguer Duplo", "Descrição", "789123", 30m, 12m, false, 15);

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnFailure()
    {
        _productRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await _handler.Handle(ValidCommand(1, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    [Fact]
    public async Task Handle_ProductInactive_ShouldReturnFailure()
    {
        var product = CreateProduct(active: false);
        _productRepository.GetByIdForUpdateAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await _handler.Handle(ValidCommand(product.Id, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ShouldReturnFailure()
    {
        var product = CreateProduct();
        _productRepository.GetByIdForUpdateAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _categoryRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Category?)null);

        var result = await _handler.Handle(ValidCommand(product.Id, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.NotFound");
    }

    [Fact]
    public async Task Handle_CategoryFromDifferentCompany_ShouldReturnFailure()
    {
        var product = CreateProduct(companyId: 4);
        _productRepository.GetByIdForUpdateAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        var category = CreateCategory(1, companyId: 99);
        _categoryRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _handler.Handle(ValidCommand(product.Id, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.NotFound");
    }

    [Fact]
    public async Task Handle_InvalidSalePrice_ShouldReturnFailure()
    {
        var product = CreateProduct(companyId: 4);
        _productRepository.GetByIdForUpdateAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        var category = CreateCategory(1, companyId: 4);
        _categoryRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(category);

        var command = ValidCommand(product.Id, 1) with { SalePrice = -1m };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.InvalidSalePrice");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldUpdateAndTriggerSync()
    {
        var product = CreateProduct(companyId: 4);
        _productRepository.GetByIdForUpdateAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        var category = CreateCategory(1, companyId: 4);
        _categoryRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _handler.Handle(ValidCommand(product.Id, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        product.Name.Should().Be("X-Burguer Duplo");
        product.SalePrice.Should().Be(30m);
        _catalogSyncTrigger.Received(1).TriggerCompanySync(4);
    }
}
