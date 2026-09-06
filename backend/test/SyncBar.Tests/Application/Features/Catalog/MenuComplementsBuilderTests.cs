using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Catalog;
using SyncBar.Application.Features.Catalog.Complements;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog;

public sealed class MenuComplementsBuilderTests
{
    private readonly IProductComplementGroupRepository _productComplementGroupRepository = Substitute.For<IProductComplementGroupRepository>();
    private readonly IComplementGroupRepository _complementGroupRepository = Substitute.For<IComplementGroupRepository>();
    private readonly IComplementItemRepository _complementItemRepository = Substitute.For<IComplementItemRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static ComplementGroup CreateGroup(long id, string name = "Bebidas")
    {
        var group = ComplementGroup.Create(1, name, ComplementGroupTypeIds.SelecaoAdicional, 0, 1).Value;
        SetId(group, id);
        return group;
    }

    private static ComplementItem CreateItem(long id, string name, long? linkedProductId = null)
    {
        var item = ComplementItem.Create(1, name, linkedProductId).Value;
        SetId(item, id);
        return item;
    }

    private static ProductComplementGroup CreateLink(long id, long productId, long groupId, int displayOrder = 0)
    {
        var link = ProductComplementGroup.Create(productId, groupId, displayOrder).Value;
        SetId(link, id);
        return link;
    }

    private static Product CreateProduct(long id, string? imageUrl)
    {
        var product = Product.Create(1, 1, 1, "Produto " + id, null, null, 10m, null, false, null).Value;
        product.SetImage(imageUrl);
        SetId(product, id);
        return product;
    }

    private Task<IReadOnlyDictionary<long, IReadOnlyCollection<ComplementGroupResponse>>> Call(
        IReadOnlyCollection<long> productIds, CancellationToken ct = default)
        => MenuComplementsBuilder.BuildAsync(
            productIds, _productComplementGroupRepository, _complementGroupRepository, _complementItemRepository, _productRepository, ct);

    [Fact]
    public async Task BuildAsync_NoProductIds_ShouldReturnEmptyWithoutCallingRepositories()
    {
        var result = await Call([]);

        result.Should().BeEmpty();
        await _productComplementGroupRepository.DidNotReceive().GetByProductsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BuildAsync_NoLinksFound_ShouldReturnEmptyWithoutFurtherCalls()
    {
        _productComplementGroupRepository.GetByProductsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProductComplementGroup>());

        var result = await Call([1, 2]);

        result.Should().BeEmpty();
        await _complementGroupRepository.DidNotReceive().GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BuildAsync_LinkToDeletedGroup_ShouldBeFilteredOut()
    {
        var link = CreateLink(1, productId: 10, groupId: 99);
        _productComplementGroupRepository.GetByProductsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([link]);
        _complementGroupRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ComplementGroup>());

        var result = await Call([10]);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildAsync_ComplementItemMissing_ShouldFallBackToQuestionMarkName()
    {
        var group = CreateGroup(1);
        group.AddComplement(complementItemId: 500, extraPrice: 2m);
        var link = CreateLink(1, productId: 10, groupId: 1);

        _productComplementGroupRepository.GetByProductsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([link]);
        _complementGroupRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([group]);
        _complementItemRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ComplementItem>());

        var result = await Call([10]);

        result.Should().ContainKey(10);
        var groupResponse = result[10].Single();
        groupResponse.Complements.Single().ComplementItemName.Should().Be("?");
    }

    [Fact]
    public async Task BuildAsync_InactiveComplement_ShouldBeExcluded()
    {
        var group = CreateGroup(1);
        var addResult = group.AddComplement(complementItemId: 500, extraPrice: 2m);
        group.RemoveComplement(addResult.Value.Id);
        var link = CreateLink(1, productId: 10, groupId: 1);

        _productComplementGroupRepository.GetByProductsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([link]);
        _complementGroupRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([group]);
        _complementItemRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([CreateItem(500, "Refrigerante")]);

        var result = await Call([10]);

        result[10].Single().Complements.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildAsync_LinkedProductFound_ShouldResolveImageUrl()
    {
        var group = CreateGroup(1);
        group.AddComplement(complementItemId: 500, extraPrice: 0m);
        var link = CreateLink(1, productId: 10, groupId: 1);
        var linkedItem = CreateItem(500, "X-Salada (combo)", linkedProductId: 777);
        var linkedProduct = CreateProduct(777, "/images/products/777.png");

        _productComplementGroupRepository.GetByProductsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([link]);
        _complementGroupRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([group]);
        _complementItemRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([linkedItem]);
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([linkedProduct]);

        var result = await Call([10]);

        var complement = result[10].Single().Complements.Single();
        complement.LinkedProductId.Should().Be(777);
        complement.LinkedProductImageUrl.Should().Be("/images/products/777.png");
    }

    [Fact]
    public async Task BuildAsync_LinkedProductNotFound_ShouldReturnNullImageUrl()
    {
        var group = CreateGroup(1);
        group.AddComplement(complementItemId: 500, extraPrice: 0m);
        var link = CreateLink(1, productId: 10, groupId: 1);
        var linkedItem = CreateItem(500, "X-Salada (combo)", linkedProductId: 777);

        _productComplementGroupRepository.GetByProductsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([link]);
        _complementGroupRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([group]);
        _complementItemRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([linkedItem]);
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Product>());

        var result = await Call([10]);

        var complement = result[10].Single().Complements.Single();
        complement.LinkedProductId.Should().Be(777);
        complement.LinkedProductImageUrl.Should().BeNull();
    }

    [Fact]
    public async Task BuildAsync_MultipleLinksForSameProduct_ShouldOrderByDisplayOrder()
    {
        var group1 = CreateGroup(1, "Segundo");
        var group2 = CreateGroup(2, "Primeiro");
        var link1 = CreateLink(1, productId: 10, groupId: 1, displayOrder: 2);
        var link2 = CreateLink(2, productId: 10, groupId: 2, displayOrder: 1);

        _productComplementGroupRepository.GetByProductsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([link1, link2]);
        _complementGroupRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([group1, group2]);
        _complementItemRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ComplementItem>());

        var result = await Call([10]);

        result[10].Select(g => g.Name).Should().ContainInOrder("Primeiro", "Segundo");
    }
}
