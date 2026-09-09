using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Catalog.ProductExtras;
using SyncBar.Application.Features.Catalog.GetProductById;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.ProductExtras;

public sealed class ProductExtrasHandlerTests
{
    private readonly IProductRepository products = Substitute.For<IProductRepository>();
    private readonly ILogTrackerRepository logs = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork work = Substitute.For<IUnitOfWork>();
    private Product Product()
    {
        var product = SyncBar.Domain.Entities.Product.Create(1, 1, 1, "Drink", null, null, 10, null, false, null).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(product, 10L);
        products.GetByIdForUpdateAsync(10, Arg.Any<CancellationToken>()).Returns(product);
        products.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(product);
        return product;
    }

    [Fact]
    public async Task AddOptionalExtra_UsesRequestedProduct()
    {
        var product = Product();
        var result = await new AddOptionalExtraCommandHandler(products, logs, work)
            .Handle(new(10, "Lemon", 3), default);
        result.IsSuccess.Should().BeTrue();
        product.OptionalExtras.Should().ContainSingle().Which.ProductId.Should().Be(10);
    }
    [Fact]
    public async Task AddOptionalExtra_RejectsMissingProduct()
    {
        var result = await new AddOptionalExtraCommandHandler(products, logs, work)
            .Handle(new(99, "Lemon", 3), default);
        result.Error.Code.Should().Be("Product.NotFound");
    }
    [Theory]
    [InlineData("", 0)]
    [InlineData("Lemon", -1)]
    public async Task AddOptionalExtra_RejectsInvalidFields(string name, int order)
    {
        var product = Product();
        var result = await new AddOptionalExtraCommandHandler(products, logs, work)
            .Handle(new(10, name, order), default);
        result.IsFailure.Should().BeTrue();
        product.OptionalExtras.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateOptionalExtra_RejectsItemOfAnotherProduct()
    {
        var product = Product();
        var item = ProductOptionalExtra.Create(99, "Lemon", 3).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(item, 5L);
        product.OptionalExtras.Add(item);
        var result = await new UpdateOptionalExtraCommandHandler(products, logs, work)
            .Handle(new(10, 5, "Changed", 1), default);
        result.IsFailure.Should().BeTrue();
        item.IsActive.Should().BeTrue();
        item.OptionalExtraName.Should().Be("Lemon");
    }
    [Fact]
    public async Task UpdateOptionalExtra_ChangesOnlyOwnedItem()
    {
        var product = Product();
        var item = ProductOptionalExtra.Create(10, "Lemon", 3).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(item, 5L);
        product.OptionalExtras.Add(item);
        var result = await new UpdateOptionalExtraCommandHandler(products, logs, work)
            .Handle(new(10, 5, "Changed", 1), default);
        result.IsSuccess.Should().BeTrue();
        item.OptionalExtraName.Should().Be("Changed");
        item.DisplayOrder.Should().Be(1);
        item.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteOptionalExtra_RejectsItemOfAnotherProduct()
    {
        var product = Product();
        var item = ProductOptionalExtra.Create(99, "Lemon", 3).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(item, 5L);
        product.OptionalExtras.Add(item);
        var result = await new DeleteOptionalExtraCommandHandler(products, logs, work)
            .Handle(new(10, 5), default);
        result.IsFailure.Should().BeTrue();
        item.IsActive.Should().BeTrue();
        item.OptionalExtraName.Should().Be("Lemon");
    }
    [Fact]
    public async Task DeleteOptionalExtra_ChangesOnlyOwnedItem()
    {
        var product = Product();
        var item = ProductOptionalExtra.Create(10, "Lemon", 3).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(item, 5L);
        product.OptionalExtras.Add(item);
        var result = await new DeleteOptionalExtraCommandHandler(products, logs, work)
            .Handle(new(10, 5), default);
        result.IsSuccess.Should().BeTrue();
        item.IsActive.Should().BeFalse();
        item.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AddProductBoost_UsesRequestedProduct()
    {
        var product = Product();
        var result = await new AddProductBoostCommandHandler(products, logs, work)
            .Handle(new(10, "Lemon", 2.50m, 3), default);
        result.IsSuccess.Should().BeTrue();
        product.Boosts.Should().ContainSingle().Which.ProductId.Should().Be(10);
    }
    [Fact]
    public async Task AddProductBoost_RejectsMissingProduct()
    {
        var result = await new AddProductBoostCommandHandler(products, logs, work)
            .Handle(new(99, "Lemon", 2.50m, 3), default);
        result.Error.Code.Should().Be("Product.NotFound");
    }
    [Theory]
    [InlineData("", 0)]
    [InlineData("Lemon", -1)]
    public async Task AddProductBoost_RejectsInvalidFields(string name, int order)
    {
        var product = Product();
        var result = await new AddProductBoostCommandHandler(products, logs, work)
            .Handle(new(10, name, 2.50m, order), default);
        result.IsFailure.Should().BeTrue();
        product.Boosts.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateProductBoost_RejectsItemOfAnotherProduct()
    {
        var product = Product();
        var item = ProductBoost.Create(99, "Lemon", 2.50m, 3).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(item, 5L);
        product.Boosts.Add(item);
        var result = await new UpdateProductBoostCommandHandler(products, logs, work)
            .Handle(new(10, 5, "Changed", 4m, 1), default);
        result.IsFailure.Should().BeTrue();
        item.IsActive.Should().BeTrue();
        item.BoostName.Should().Be("Lemon");
    }
    [Fact]
    public async Task UpdateProductBoost_ChangesOnlyOwnedItem()
    {
        var product = Product();
        var item = ProductBoost.Create(10, "Lemon", 2.50m, 3).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(item, 5L);
        product.Boosts.Add(item);
        var result = await new UpdateProductBoostCommandHandler(products, logs, work)
            .Handle(new(10, 5, "Changed", 4m, 1), default);
        result.IsSuccess.Should().BeTrue();
        item.BoostName.Should().Be("Changed");
        item.DisplayOrder.Should().Be(1);
        item.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteProductBoost_RejectsItemOfAnotherProduct()
    {
        var product = Product();
        var item = ProductBoost.Create(99, "Lemon", 2.50m, 3).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(item, 5L);
        product.Boosts.Add(item);
        var result = await new DeleteProductBoostCommandHandler(products, logs, work)
            .Handle(new(10, 5), default);
        result.IsFailure.Should().BeTrue();
        item.IsActive.Should().BeTrue();
        item.BoostName.Should().Be("Lemon");
    }
    [Fact]
    public async Task DeleteProductBoost_ChangesOnlyOwnedItem()
    {
        var product = Product();
        var item = ProductBoost.Create(10, "Lemon", 2.50m, 3).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(item, 5L);
        product.Boosts.Add(item);
        var result = await new DeleteProductBoostCommandHandler(products, logs, work)
            .Handle(new(10, 5), default);
        result.IsSuccess.Should().BeTrue();
        item.IsActive.Should().BeFalse();
        item.UpdatedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1.001)]
    public async Task Boost_RejectsInvalidMoney(decimal value)
    {
        var product = Product();
        var result = await new AddProductBoostCommandHandler(products, logs, work).Handle(new(10, "Extra", value, 0), default);
        result.IsFailure.Should().BeTrue();
        product.Boosts.Should().BeEmpty();
    }

    [Fact]
    public async Task Toggle_PreservesItemsAndControlsCustomerResponse()
    {
        var product = Product();
        product.OptionalExtras.Add(ProductOptionalExtra.Create(10, "Late", 9).Value);
        product.OptionalExtras.Add(ProductOptionalExtra.Create(10, "First", 1).Value);
        var removed = ProductOptionalExtra.Create(10, "Removed", 0).Value;
        removed.Deactivate();
        product.OptionalExtras.Add(removed);
        product.Boosts.Add(ProductBoost.Create(10, "Paid", 2m, 0).Value);
        var handler = new ToggleProductExtrasAndBoostsCommandHandler(products, logs, work);
        (await handler.Handle(new(10, true, true), default)).IsSuccess.Should().BeTrue();
        var reader = new GetProductByIdQueryHandler(products, logs, work);
        var enabled = (await reader.Handle(new(10), default)).Value;
        enabled.OptionalExtras.Select(x => x.OptionalExtraName).Should().Equal("First", "Late");
        enabled.Boosts.Should().ContainSingle();
        await handler.Handle(new(10, false, false), default);
        var disabled = (await reader.Handle(new(10), default)).Value;
        disabled.OptionalExtras.Should().BeEmpty();
        disabled.Boosts.Should().BeEmpty();
        product.OptionalExtras.Should().HaveCount(3);
        var management = await new GetOptionalExtrasByProductIdQueryHandler(products, logs, work).Handle(new(10), default);
        management.Value.Select(x => x.DisplayOrder).Should().Equal(1, 9);
        var boosts = await new GetProductBoostsByProductIdQueryHandler(products, logs, work).Handle(new(10), default);
        boosts.Value.Should().ContainSingle();
    }

    [Fact]
    public async Task Toggle_RejectsMissingProduct()
    {
        var result = await new ToggleProductExtrasAndBoostsCommandHandler(products, logs, work).Handle(new(99, true, true), default);
        result.Error.Code.Should().Be("Product.NotFound");
    }
}

