using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Catalog.Complements.GetComplementGroups;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.Complements.GetComplementGroups;

public sealed class GetComplementGroupsQueryHandlerTests
{
    private readonly IComplementGroupRepository _complementGroupRepository = Substitute.For<IComplementGroupRepository>();
    private readonly IComplementItemRepository _complementItemRepository = Substitute.For<IComplementItemRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetComplementGroupsQueryHandler _handler;

    public GetComplementGroupsQueryHandlerTests()
    {
        _handler = new GetComplementGroupsQueryHandler(
            _complementGroupRepository, _complementItemRepository, _productRepository, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static ComplementGroup CreateGroup(long id, string name)
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

    private static Product CreateProduct(long id, string? imageUrl)
    {
        var product = Product.Create(1, 1, 1, "Produto " + id, null, null, 10m, null, false, null).Value;
        product.SetImage(imageUrl);
        SetId(product, id);
        return product;
    }

    [Fact]
    public async Task Handle_NoGroups_ShouldReturnEmpty()
    {
        _complementGroupRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(Array.Empty<ComplementGroup>());
        _complementItemRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ComplementItem>());

        var result = await _handler.Handle(new GetComplementGroupsQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldOrderByNameAndFallBackToQuestionMarkForMissingItem()
    {
        var group1 = CreateGroup(1, "Zebra");
        group1.AddComplement(500, 1m);
        var group2 = CreateGroup(2, "Alfa");

        _complementGroupRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns([group1, group2]);
        _complementItemRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ComplementItem>());

        var result = await _handler.Handle(new GetComplementGroupsQuery(1), CancellationToken.None);

        result.Value.Select(g => g.Name).Should().ContainInOrder("Alfa", "Zebra");
        result.Value.Single(g => g.Name == "Zebra").Complements.Single().ComplementItemName.Should().Be("?");
    }

    [Fact]
    public async Task Handle_ComplementWithLinkedProduct_ShouldResolveImageUrl()
    {
        var group = CreateGroup(1, "Combo");
        group.AddComplement(500, 0m);
        var item = CreateItem(500, "X-Salada (combo)", linkedProductId: 777);
        var product = CreateProduct(777, "/images/777.png");

        _complementGroupRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns([group]);
        _complementItemRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([item]);
        _productRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([product]);

        var result = await _handler.Handle(new GetComplementGroupsQuery(1), CancellationToken.None);

        var complement = result.Value.Single().Complements.Single();
        complement.ComplementItemName.Should().Be("X-Salada (combo)");
        complement.LinkedProductImageUrl.Should().Be("/images/777.png");
    }

    [Fact]
    public async Task Handle_InactiveComplement_ShouldBeExcluded()
    {
        var group = CreateGroup(1, "Combo");
        var added = group.AddComplement(500, 0m);
        group.RemoveComplement(added.Value.Id);

        _complementGroupRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns([group]);
        _complementItemRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ComplementItem>());

        var result = await _handler.Handle(new GetComplementGroupsQuery(1), CancellationToken.None);

        result.Value.Single().Complements.Should().BeEmpty();
    }
}
