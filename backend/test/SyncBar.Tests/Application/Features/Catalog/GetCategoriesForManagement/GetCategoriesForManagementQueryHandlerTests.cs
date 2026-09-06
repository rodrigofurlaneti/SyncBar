using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Catalog.GetCategoriesForManagement;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.GetCategoriesForManagement;

public sealed class GetCategoriesForManagementQueryHandlerTests
{
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetCategoriesForManagementQueryHandler _handler;

    public GetCategoriesForManagementQueryHandlerTests()
    {
        _handler = new GetCategoriesForManagementQueryHandler(_categoryRepository, _productRepository, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static Category CreateCategory(long id, string name, int displayOrder, bool active = true)
    {
        var category = Category.Create(1, name, displayOrder).Value;
        if (!active)
            category.Deactivate();
        SetId(category, id);
        return category;
    }

    private static Product CreateProduct(long id, long categoryId)
    {
        var product = Product.Create(1, categoryId, 1, "Produto " + id, null, null, 10m, null, false, null).Value;
        SetId(product, id);
        return product;
    }

    [Fact]
    public async Task Handle_ShouldReturnCategoriesWithProductCountsOrderedByDisplayOrderThenName()
    {
        var categories = new[]
        {
            CreateCategory(1, "Sobremesas", 2, active: true),
            CreateCategory(2, "Bebidas", 1, active: false),
        };
        var products = new[]
        {
            CreateProduct(10, categoryId: 1),
            CreateProduct(11, categoryId: 1),
            CreateProduct(12, categoryId: 2),
        };
        _categoryRepository.GetAllByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(categories);
        _productRepository.GetAllByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(products);

        var result = await _handler.Handle(new GetCategoriesForManagementQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(c => c.Name).Should().ContainInOrder("Bebidas", "Sobremesas");
        result.Value.Single(c => c.Name == "Sobremesas").ProductCount.Should().Be(2);
        result.Value.Single(c => c.Name == "Bebidas").ProductCount.Should().Be(1);
        result.Value.Single(c => c.Name == "Bebidas").IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CategoryWithNoProducts_ShouldReturnZeroCount()
    {
        var categories = new[] { CreateCategory(1, "Bebidas", 1) };
        _categoryRepository.GetAllByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(categories);
        _productRepository.GetAllByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(Array.Empty<Product>());

        var result = await _handler.Handle(new GetCategoriesForManagementQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Single().ProductCount.Should().Be(0);
    }
}
