using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.Complements.AddComplement;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.Complements.AddComplement;

public sealed class AddComplementCommandHandlerTests
{
    private readonly IComplementGroupRepository _complementGroupRepository = Substitute.For<IComplementGroupRepository>();
    private readonly IComplementItemRepository _complementItemRepository = Substitute.For<IComplementItemRepository>();
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger = Substitute.For<IIfoodCatalogSyncTrigger>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AddComplementCommandHandler _handler;

    public AddComplementCommandHandlerTests()
    {
        _handler = new AddComplementCommandHandler(
            _complementGroupRepository, _complementItemRepository, _catalogSyncTrigger, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static ComplementGroup CreateGroup(long id = 1, long companyId = 1, bool active = true)
    {
        var group = ComplementGroup.Create(companyId, "Bebidas", ComplementGroupTypeIds.SelecaoAdicional, 0, 1).Value;
        if (!active)
            group.Deactivate();
        SetId(group, id);
        return group;
    }

    private static ComplementItem CreateItem(long id = 1, long companyId = 1, bool active = true)
    {
        var item = ComplementItem.Create(companyId, "Refrigerante").Value;
        if (!active)
            item.Deactivate();
        SetId(item, id);
        return item;
    }

    [Fact]
    public async Task Handle_GroupNotFound_ShouldReturnFailure()
    {
        _complementGroupRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((ComplementGroup?)null);

        var result = await _handler.Handle(new AddComplementCommand(1, 1, 2m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.NotFound");
    }

    [Fact]
    public async Task Handle_GroupInactive_ShouldReturnFailure()
    {
        var group = CreateGroup(active: false);
        _complementGroupRepository.GetByIdForUpdateAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);

        var result = await _handler.Handle(new AddComplementCommand(group.Id, 1, 2m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.NotFound");
    }

    [Fact]
    public async Task Handle_ItemNotFound_ShouldReturnFailure()
    {
        var group = CreateGroup();
        _complementGroupRepository.GetByIdForUpdateAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);
        _complementItemRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((ComplementItem?)null);

        var result = await _handler.Handle(new AddComplementCommand(group.Id, 1, 2m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementItem.NotFound");
    }

    [Fact]
    public async Task Handle_ItemFromDifferentCompany_ShouldReturnFailure()
    {
        var group = CreateGroup(companyId: 1);
        _complementGroupRepository.GetByIdForUpdateAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);
        var item = CreateItem(companyId: 99);
        _complementItemRepository.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _handler.Handle(new AddComplementCommand(group.Id, item.Id, 2m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementItem.NotFound");
    }

    [Fact]
    public async Task Handle_DuplicateComplement_ShouldReturnFailure()
    {
        var group = CreateGroup(companyId: 1);
        var item = CreateItem(id: 5, companyId: 1);
        group.AddComplement(item.Id, 1m);
        _complementGroupRepository.GetByIdForUpdateAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);
        _complementItemRepository.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _handler.Handle(new AddComplementCommand(group.Id, item.Id, 2m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.DuplicateComplementItem");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldAddComplementAndTriggerSync()
    {
        var group = CreateGroup(companyId: 7);
        var item = CreateItem(id: 5, companyId: 7);
        _complementGroupRepository.GetByIdForUpdateAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);
        _complementItemRepository.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _handler.Handle(new AddComplementCommand(group.Id, item.Id, 3m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        group.Complements.Should().ContainSingle(c => c.ComplementItemId == item.Id && c.ExtraPrice == 3m);
        _catalogSyncTrigger.Received(1).TriggerCompanySync(7);
    }
}
