using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.PublicOrdering.GetPublicMenu;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.PublicOrdering.GetPublicMenu;

public sealed class GetPublicMenuQueryHandlerTests
{
    private const long BranchId = 1;
    private const long CompanyId = 1;

    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly IProductComplementGroupRepository _productComplementGroupRepository = Substitute.For<IProductComplementGroupRepository>();
    private readonly IComplementGroupRepository _complementGroupRepository = Substitute.For<IComplementGroupRepository>();
    private readonly IComplementItemRepository _complementItemRepository = Substitute.For<IComplementItemRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetPublicMenuQueryHandler _handler;

    public GetPublicMenuQueryHandlerTests()
    {
        _handler = new GetPublicMenuQueryHandler(
            _diningTableRepository, _branchRepository, _productRepository, _categoryRepository,
            _productComplementGroupRepository, _complementGroupRepository, _complementItemRepository,
            _logRepository, _unitOfWork);

        _productComplementGroupRepository.GetByProductsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup>());
        _categoryRepository.GetByCompanyAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category>());
    }

    private static DiningTable MakeTable(int number = 7) => DiningTable.Create(BranchId, 1, number, 4).Value;

    private static Branch MakeBranch() => Branch.Create(CompanyId, "Filial Centro", null, null, null, null, null, null, null, null).Value;

    private static Product MakeProduct(long id, long categoryId, string name, decimal salePrice = 10m)
    {
        var product = Product.Create(CompanyId, categoryId, 1, name, null, null, salePrice, null, false, null).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(product, id);
        return product;
    }

    private static Category MakeCategory(long id, string name)
    {
        var category = Category.Create(CompanyId, name, 0).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(category, id);
        return category;
    }

    private void SetupTableAndBranch(Guid token, DiningTable table, Branch branch)
    {
        _diningTableRepository.GetByQrTokenAsync(token, Arg.Any<CancellationToken>()).Returns(table);
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns(branch);
    }

    [Fact]
    public async Task Handle_InvalidToken_ShouldReturnFailure()
    {
        var query = new GetPublicMenuQuery(Guid.NewGuid());
        _diningTableRepository.GetByQrTokenAsync(query.Token, Arg.Any<CancellationToken>()).Returns((DiningTable?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningTable.InvalidToken");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BranchNotFound_ShouldReturnFailure()
    {
        var table = MakeTable();
        var query = new GetPublicMenuQuery(Guid.NewGuid());
        _diningTableRepository.GetByQrTokenAsync(query.Token, Arg.Any<CancellationToken>()).Returns(table);
        _branchRepository.GetByIdAsync(BranchId, Arg.Any<CancellationToken>()).Returns((Branch?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InactiveBranch_ShouldReturnFailure()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        branch.Deactivate();
        var query = new GetPublicMenuQuery(Guid.NewGuid());
        SetupTableAndBranch(query.Token, table, branch);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoProducts_ShouldReturnEmptyMenuWithBranchAndTableInfoAndSkipComplementLookup()
    {
        var table = MakeTable(number: 7);
        var branch = MakeBranch();
        var query = new GetPublicMenuQuery(Guid.NewGuid());
        SetupTableAndBranch(query.Token, table, branch);
        _productRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(new List<Product>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BranchName.Should().Be("Filial Centro");
        result.Value.TableNumber.Should().Be(7);
        result.Value.Items.Should().BeEmpty();
        await _productComplementGroupRepository.DidNotReceiveWithAnyArgs().GetByProductsAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnItemsOrderedByCategoryThenNameWithTableFlags()
    {
        var table = MakeTable();
        table.SetReadingValidationSettings(isCameraInputEnabled: true, isBarcodeEnabled: true, isQrCodeEnabled: true);
        var branch = MakeBranch();
        var productCerveja = MakeProduct(1, categoryId: 1, name: "Cerveja");
        var productBatata = MakeProduct(2, categoryId: 1, name: "Batata");
        var productTorta = MakeProduct(3, categoryId: 2, name: "Torta");
        var categoryBebidas = MakeCategory(1, "Bebidas");
        var categorySobremesas = MakeCategory(2, "Sobremesas");
        var query = new GetPublicMenuQuery(Guid.NewGuid());
        SetupTableAndBranch(query.Token, table, branch);
        _productRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>())
            .Returns(new List<Product> { productCerveja, productBatata, productTorta });
        _categoryRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>())
            .Returns(new List<Category> { categoryBebidas, categorySobremesas });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var menu = result.Value;
        menu.Items.Should().HaveCount(3);
        menu.Items.Select(i => i.Name).Should().ContainInOrder("Batata", "Cerveja", "Torta");
        menu.Items.First(i => i.Id == 1).CategoryName.Should().Be("Bebidas");
        menu.Items.First(i => i.Id == 3).CategoryName.Should().Be("Sobremesas");
        menu.IsCameraInputEnabled.Should().BeTrue();
        menu.IsBarcodeEnabled.Should().BeTrue();
        menu.IsQrCodeEnabled.Should().BeTrue();
        menu.IsQrViewEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ProductWithUnknownCategory_ShouldUseFallbackCategoryName()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct(1, categoryId: 999, name: "Item Órfão");
        var query = new GetPublicMenuQuery(Guid.NewGuid());
        SetupTableAndBranch(query.Token, table, branch);
        _productRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(new List<Product> { product });
        _categoryRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(new List<Category>());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(i => i.CategoryName == "Geral");
    }

    [Fact]
    public async Task Handle_ProductWithNoComplementLinks_ShouldReturnEmptyComplementGroupsList()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct(1, categoryId: 1, name: "X-Burger");
        var query = new GetPublicMenuQuery(Guid.NewGuid());
        SetupTableAndBranch(query.Token, table, branch);
        _productRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(new List<Product> { product });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items.First().ComplementGroups.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ProductWithComplementGroup_ShouldIncludeComplementGroupAndComplementInResponse()
    {
        var table = MakeTable();
        var branch = MakeBranch();
        var product = MakeProduct(501, categoryId: 1, name: "X-Burger");
        var link = ProductComplementGroup.Create(501, 777, 0).Value;
        var group = ComplementGroup.Create(CompanyId, "Adicionais", ComplementGroupTypeIds.SelecaoAdicional, 0, 3).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(group, 777L);
        var complement = group.AddComplement(complementItemId: 42, extraPrice: 2.5m).Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(complement, 900L);
        var complementItem = ComplementItem.Create(CompanyId, "Bacon").Value;
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(complementItem, 42L);
        var query = new GetPublicMenuQuery(Guid.NewGuid());
        SetupTableAndBranch(query.Token, table, branch);
        _productRepository.GetByCompanyAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(new List<Product> { product });
        _productComplementGroupRepository.GetByProductsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProductComplementGroup> { link });
        _complementGroupRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ComplementGroup> { group });
        _complementItemRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ComplementItem> { complementItem });

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items.Should().ContainSingle().Subject;
        var complementGroup = item.ComplementGroups.Should().ContainSingle().Subject;
        complementGroup.Id.Should().Be(777);
        complementGroup.Name.Should().Be("Adicionais");
        var complementResponse = complementGroup.Complements.Should().ContainSingle().Subject;
        complementResponse.ComplementItemName.Should().Be("Bacon");
        complementResponse.ExtraPrice.Should().Be(2.5m);
    }
}
