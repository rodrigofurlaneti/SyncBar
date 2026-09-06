using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.Complements.UpdateComplementItem;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.Complements.UpdateComplementItem;

public sealed class UpdateComplementItemCommandHandlerTests
{
    private readonly IComplementItemRepository _complementItemRepository = Substitute.For<IComplementItemRepository>();
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger = Substitute.For<IIfoodCatalogSyncTrigger>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateComplementItemCommandHandler _handler;

    public UpdateComplementItemCommandHandlerTests()
    {
        _handler = new UpdateComplementItemCommandHandler(_complementItemRepository, _catalogSyncTrigger, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static ComplementItem CreateItem(long id = 1, long companyId = 1, bool active = true)
    {
        var item = ComplementItem.Create(companyId, "Bacon").Value;
        if (!active)
            item.Deactivate();
        SetId(item, id);
        return item;
    }

    [Fact]
    public async Task Handle_ItemNotFound_ShouldReturnFailure()
    {
        _complementItemRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((ComplementItem?)null);

        var result = await _handler.Handle(new UpdateComplementItemCommand(1, "Bacon Extra"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementItem.NotFound");
    }

    [Fact]
    public async Task Handle_EmptyName_ShouldReturnFailure()
    {
        var item = CreateItem();
        _complementItemRepository.GetByIdForUpdateAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _handler.Handle(new UpdateComplementItemCommand(item.Id, ""), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementItem.EmptyName");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldUpdateAndTriggerSync()
    {
        var item = CreateItem(companyId: 9);
        _complementItemRepository.GetByIdForUpdateAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _handler.Handle(new UpdateComplementItemCommand(item.Id, "Bacon Extra"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.Name.Should().Be("Bacon Extra");
        _catalogSyncTrigger.Received(1).TriggerCompanySync(9);
    }
}
