using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.Complements.UpdateComplementGroup;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.Complements.UpdateComplementGroup;

public sealed class UpdateComplementGroupCommandHandlerTests
{
    private readonly IComplementGroupRepository _complementGroupRepository = Substitute.For<IComplementGroupRepository>();
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger = Substitute.For<IIfoodCatalogSyncTrigger>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateComplementGroupCommandHandler _handler;

    public UpdateComplementGroupCommandHandlerTests()
    {
        _handler = new UpdateComplementGroupCommandHandler(_complementGroupRepository, _catalogSyncTrigger, _logRepository, _unitOfWork);
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

    [Fact]
    public async Task Handle_GroupNotFound_ShouldReturnFailure()
    {
        _complementGroupRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((ComplementGroup?)null);

        var result = await _handler.Handle(
            new UpdateComplementGroupCommand(1, "Bebidas", ComplementGroupTypeIds.SelecaoAdicional, 0, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.NotFound");
    }

    [Fact]
    public async Task Handle_InvalidDetails_ShouldReturnFailure()
    {
        var group = CreateGroup();
        _complementGroupRepository.GetByIdForUpdateAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);

        var result = await _handler.Handle(
            new UpdateComplementGroupCommand(group.Id, "", ComplementGroupTypeIds.SelecaoAdicional, 0, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.EmptyName");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldUpdateAndTriggerSync()
    {
        var group = CreateGroup(companyId: 3);
        _complementGroupRepository.GetByIdForUpdateAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);

        var result = await _handler.Handle(
            new UpdateComplementGroupCommand(group.Id, "Refrigerantes", ComplementGroupTypeIds.Ingredientes, 1, 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        group.Name.Should().Be("Refrigerantes");
        group.MaxSelection.Should().Be(2);
        _catalogSyncTrigger.Received(1).TriggerCompanySync(3);
    }
}
