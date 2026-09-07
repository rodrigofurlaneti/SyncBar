using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Storefront.GetBranchMenu;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Storefront.GetBranchMenu;

public sealed class GetBranchMenuQueryHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IProductComplementGroupRepository _productComplementGroupRepository = Substitute.For<IProductComplementGroupRepository>();
    private readonly IComplementGroupRepository _complementGroupRepository = Substitute.For<IComplementGroupRepository>();
    private readonly IComplementItemRepository _complementItemRepository = Substitute.For<IComplementItemRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetBranchMenuQueryHandler _handler;

    public GetBranchMenuQueryHandlerTests()
    {
        _handler = new GetBranchMenuQueryHandler(
            _branchRepository, _productRepository, _categoryRepository, _productComplementGroupRepository,
            _complementGroupRepository, _complementItemRepository, _logRepository, _unitOfWork);

        _productComplementGroupRepository.GetByProductsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProductComplementGroup>());
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static Branch CreateBranch(long companyId = 1, string name = "Filial Centro")
        => Branch.Create(companyId, name, null, null, null, null, null, null, null, null).Value;

    private static Product CreateProduct(long id, long categoryId, string name, long companyId = 1)
    {
        var product = Product.Create(companyId, categoryId, 1, name, null, null, 10m, null, false, null).Value;
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
    public async Task Handle_BranchNotFound_ShouldReturnFailure()
    {
        var query = new GetBranchMenuQuery(1);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
        await _productRepository.DidNotReceive().GetByCompanyAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BranchInactive_ShouldReturnFailure()
    {
        var branch = CreateBranch();
        branch.Deactivate();
        var query = new GetBranchMenuQuery(1);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(branch);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
    }

    [Fact]
    public async Task Handle_ProductWithKnownCategory_ShouldMapCategoryNameAndBranchName()
    {
        var branch = CreateBranch(name: "Filial Zona Sul");
        var product = CreateProduct(1, categoryId: 5, name: "X-Burguer");
        var query = new GetBranchMenuQuery(1);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(branch);
        _productRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns([product]);
        _categoryRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns([CreateCategory(5, "Lanches")]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BranchName.Should().Be("Filial Zona Sul");
        result.Value.Items.Single().CategoryName.Should().Be("Lanches");
        result.Value.Items.Single().ComplementGroups.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ProductWithUnknownCategory_ShouldFallBackToGeral()
    {
        var branch = CreateBranch();
        var product = CreateProduct(1, categoryId: 99, name: "X-Burguer");
        var query = new GetBranchMenuQuery(1);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(branch);
        _productRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns([product]);
        _categoryRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(Array.Empty<Category>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Single().CategoryName.Should().Be("Geral");
    }

    [Fact]
    public async Task Handle_MultipleProducts_ShouldOrderByCategoryThenName()
    {
        var branch = CreateBranch();
        var products = new[]
        {
            CreateProduct(1, categoryId: 2, name: "Z-Item"),
            CreateProduct(2, categoryId: 1, name: "A-Item"),
            CreateProduct(3, categoryId: 1, name: "B-Item"),
        };
        var query = new GetBranchMenuQuery(1);
        _branchRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(branch);
        _productRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(products);
        _categoryRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(Array.Empty<Category>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Select(p => p.Name).Should().ContainInOrder("A-Item", "B-Item", "Z-Item");
    }
}
