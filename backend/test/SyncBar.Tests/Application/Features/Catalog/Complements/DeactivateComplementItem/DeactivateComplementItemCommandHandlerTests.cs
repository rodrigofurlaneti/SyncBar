using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Catalog.Complements.DeactivateComplementItem;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.Complements.DeactivateComplementItem;

public sealed class DeactivateComplementItemCommandHandlerTests
{
    private readonly IComplementItemRepository _complementItemRepository = Substitute.For<IComplementItemRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeactivateComplementItemCommandHandler _handler;

    public DeactivateComplementItemCommandHandlerTests()
    {
        _handler = new DeactivateComplementItemCommandHandler(_complementItemRepository, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static ComplementItem CreateItem(long id = 1, bool active = true)
    {
        var item = ComplementItem.Create(1, "Bacon").Value;
        if (!active)
            item.Deactivate();
        SetId(item, id);
        return item;
    }

    [Fact]
    public async Task Handle_ItemNotFound_ShouldReturnFailure()
    {
        _complementItemRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((ComplementItem?)null);

        var result = await _handler.Handle(new DeactivateComplementItemCommand(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementItem.NotFound");
    }

    [Fact]
    public async Task Handle_AlreadyInactive_ShouldReturnFailure()
    {
        var item = CreateItem(active: false);
        _complementItemRepository.GetByIdForUpdateAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _handler.Handle(new DeactivateComplementItemCommand(item.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementItem.NotFound");
    }

    [Fact]
    public async Task Handle_Active_ShouldDeactivate()
    {
        var item = CreateItem();
        _complementItemRepository.GetByIdForUpdateAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _handler.Handle(new DeactivateComplementItemCommand(item.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.IsActive.Should().BeFalse();
    }
}
