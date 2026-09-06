using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Catalog.GetMenu;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.GetMenu;

public sealed class GetMenuQueryHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IProductComplementGroupRepository _productComplementGroupRepository = Substitute.For<IProductComplementGroupRepository>();
    private readonly IComplementGroupRepository _complementGroupRepository = Substitute.For<IComplementGroupRepository>();
    private readonly IComplementItemRepository _complementItemRepository = Substitute.For<IComplementItemRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetMenuQueryHandler _handler;

    public GetMenuQueryHandlerTests()
    {
        _handler = new GetMenuQueryHandler(
            _productRepository, _categoryRepository, _productComplementGroupRepository,
            _complementGroupRepository, _complementItemRepository, _logRepository, _unitOfWork);

        _productComplementGroupRepository.GetByProductsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProductComplementGroup>());
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static Product CreateProduct(long id, long categoryId, string name)
    {
        var product = Product.Create(1, categoryId, 1, name, null, null, 10m, null, false, null).Value;
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
        var product = CreateProduct(1, categoryId: 5, "X-Burguer");
        _productRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns([product]);
        _categoryRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns([CreateCategory(5, "Lanches")]);

        var result = await _handler.Handle(new GetMenuQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Single().CategoryName.Should().Be("Lanches");
        result.Value.Single().ComplementGroups.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ProductWithUnknownCategory_ShouldFallBackToGeral()
    {
        var product = CreateProduct(1, categoryId: 99, "X-Burguer");
        _productRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns([product]);
        _categoryRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(Array.Empty<Category>());

        var result = await _handler.Handle(new GetMenuQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Single().CategoryName.Should().Be("Geral");
    }

    [Fact]
    public async Task Handle_ShouldOrderByCategoryThenName()
    {
        var products = new[]
        {
            CreateProduct(1, categoryId: 2, "Z-Item"),
            CreateProduct(2, categoryId: 1, "A-Item"),
            CreateProduct(3, categoryId: 1, "B-Item"),
        };
        _productRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(products);
        _categoryRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(Array.Empty<Category>());

        var result = await _handler.Handle(new GetMenuQuery(1), CancellationToken.None);

        result.Value.Select(p => p.Name).Should().ContainInOrder("A-Item", "B-Item", "Z-Item");
    }
}
