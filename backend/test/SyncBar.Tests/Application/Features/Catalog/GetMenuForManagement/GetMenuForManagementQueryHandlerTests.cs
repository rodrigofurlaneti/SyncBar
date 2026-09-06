using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Catalog.GetMenuForManagement;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.GetMenuForManagement;

public sealed class GetMenuForManagementQueryHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetMenuForManagementQueryHandler _handler;

    public GetMenuForManagementQueryHandlerTests()
    {
        _handler = new GetMenuForManagementQueryHandler(_productRepository, _categoryRepository, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static Product CreateProduct(long id, long categoryId, string name, bool active = true)
    {
        var product = Product.Create(1, categoryId, 1, name, null, null, 10m, null, false, null).Value;
        if (!active)
            product.Deactivate();
        SetId(product, id);
        return product;
    }

    private static Category CreateCategory(long id, string name)
    {
        var category = Category.Create(1, name, 0).Value;
        SetId(category, id);
        return category;
    }

    [Fact]
    public async Task Handle_ProductWithKnownCategory_ShouldMapCategoryName()
    {
        var product = CreateProduct(1, categoryId: 5, "X-Burguer", active: false);
        _productRepository.GetAllByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns([product]);
        _categoryRepository.GetAllByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns([CreateCategory(5, "Lanches")]);

        var result = await _handler.Handle(new GetMenuForManagementQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var response = result.Value.Single();
        response.CategoryName.Should().Be("Lanches");
        response.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ProductWithRemovedCategory_ShouldFallBackToCategoriaRemovida()
    {
        var product = CreateProduct(1, categoryId: 99, "X-Burguer");
        _productRepository.GetAllByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns([product]);
        _categoryRepository.GetAllByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(Array.Empty<Category>());

        var result = await _handler.Handle(new GetMenuForManagementQuery(1), CancellationToken.None);

        result.Value.Single().CategoryName.Should().Be("Categoria removida");
    }

    [Fact]
    public async Task Handle_ShouldOrderByCategoryThenName()
    {
        var products = new[]
        {
            CreateProduct(1, categoryId: 2, "Z-Item"),
            CreateProduct(2, categoryId: 1, "A-Item"),
        };
        _productRepository.GetAllByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(products);
        _categoryRepository.GetAllByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(Array.Empty<Category>());

        var result = await _handler.Handle(new GetMenuForManagementQuery(1), CancellationToken.None);

        result.Value.Select(p => p.Name).Should().ContainInOrder("A-Item", "Z-Item");
    }
}
