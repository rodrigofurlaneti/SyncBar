using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Catalog.Complements.GetComplementItems;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.Complements.GetComplementItems;

public sealed class GetComplementItemsQueryHandlerTests
{
    private readonly IComplementItemRepository _complementItemRepository = Substitute.For<IComplementItemRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetComplementItemsQueryHandler _handler;

    public GetComplementItemsQueryHandlerTests()
    {
        _handler = new GetComplementItemsQueryHandler(_complementItemRepository, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static ComplementItem CreateItem(long id, string name, bool active = true)
    {
        var item = ComplementItem.Create(1, name).Value;
        if (!active)
            item.Deactivate();
        SetId(item, id);
        return item;
    }

    [Fact]
    public async Task Handle_ShouldReturnItemsOrderedByName()
    {
        var items = new[]
        {
            CreateItem(1, "Bacon"),
            CreateItem(2, "Alface", active: false),
        };
        _complementItemRepository.GetByCompanyAsync(1, Arg.Any<CancellationToken>()).Returns(items);

        var result = await _handler.Handle(new GetComplementItemsQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(i => i.Name).Should().ContainInOrder("Alface", "Bacon");
        result.Value.Single(i => i.Name == "Alface").IsActive.Should().BeFalse();
    }
}
