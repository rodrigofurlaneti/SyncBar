using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Catalog.Complements.GetProductComplementGroups;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.Complements.GetProductComplementGroups;

public sealed class GetProductComplementGroupsQueryHandlerTests
{
    private readonly IProductComplementGroupRepository _productComplementGroupRepository = Substitute.For<IProductComplementGroupRepository>();
    private readonly IComplementGroupRepository _complementGroupRepository = Substitute.For<IComplementGroupRepository>();
    private readonly IComplementItemRepository _complementItemRepository = Substitute.For<IComplementItemRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetProductComplementGroupsQueryHandler _handler;

    public GetProductComplementGroupsQueryHandlerTests()
    {
        _handler = new GetProductComplementGroupsQueryHandler(
            _productComplementGroupRepository, _complementGroupRepository, _complementItemRepository, _productRepository,
            _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static ComplementGroup CreateGroup(long id, string name)
    {
        var group = ComplementGroup.Create(1, name, ComplementGroupTypeIds.SelecaoAdicional, 0, 1).Value;
        SetId(group, id);
        return group;
    }

    private static ProductComplementGroup CreateLink(long id, long productId, long groupId, int displayOrder)
    {
        var link = ProductComplementGroup.Create(productId, groupId, displayOrder).Value;
        SetId(link, id);
        return link;
    }

    [Fact]
    public async Task Handle_NoLinks_ShouldReturnEmptyWithoutFurtherCalls()
    {
        _productComplementGroupRepository.GetByProductAsync(1, Arg.Any<CancellationToken>()).Returns(Array.Empty<ProductComplementGroup>());

        var result = await _handler.Handle(new GetProductComplementGroupsQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _complementGroupRepository.DidNotReceive().GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LinkToDeletedGroup_ShouldBeFilteredOut()
    {
        var link = CreateLink(1, productId: 1, groupId: 99, displayOrder: 0);
        _productComplementGroupRepository.GetByProductAsync(1, Arg.Any<CancellationToken>()).Returns([link]);
        _complementGroupRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ComplementGroup>());
        _complementItemRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ComplementItem>());

        var result = await _handler.Handle(new GetProductComplementGroupsQuery(1), CancellationToken.None);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldOrderByDisplayOrderAndMapFields()
    {
        var group1 = CreateGroup(1, "Segundo");
        var group2 = CreateGroup(2, "Primeiro");
        var link1 = CreateLink(1, productId: 1, groupId: 1, displayOrder: 2);
        var link2 = CreateLink(2, productId: 1, groupId: 2, displayOrder: 1);

        _productComplementGroupRepository.GetByProductAsync(1, Arg.Any<CancellationToken>()).Returns([link1, link2]);
        _complementGroupRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns([group1, group2]);
        _complementItemRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ComplementItem>());

        var result = await _handler.Handle(new GetProductComplementGroupsQuery(1), CancellationToken.None);

        result.Value.Select(g => g.ComplementGroupName).Should().ContainInOrder("Primeiro", "Segundo");
        var first = result.Value.First();
        first.ProductComplementGroupId.Should().Be(2);
        first.DisplayOrder.Should().Be(1);
    }
}
