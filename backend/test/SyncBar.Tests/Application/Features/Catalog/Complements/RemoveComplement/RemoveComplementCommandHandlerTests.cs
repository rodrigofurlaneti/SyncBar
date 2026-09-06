using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.Complements.RemoveComplement;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.Complements.RemoveComplement;

public sealed class RemoveComplementCommandHandlerTests
{
    private readonly IComplementGroupRepository _complementGroupRepository = Substitute.For<IComplementGroupRepository>();
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger = Substitute.For<IIfoodCatalogSyncTrigger>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RemoveComplementCommandHandler _handler;

    public RemoveComplementCommandHandlerTests()
    {
        _handler = new RemoveComplementCommandHandler(_complementGroupRepository, _catalogSyncTrigger, _logRepository, _unitOfWork);
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

        var result = await _handler.Handle(new RemoveComplementCommand(1, 5), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.NotFound");
    }

    [Fact]
    public async Task Handle_ComplementNotFound_ShouldReturnFailure()
    {
        var group = CreateGroup();
        _complementGroupRepository.GetByIdForUpdateAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);

        var result = await _handler.Handle(new RemoveComplementCommand(group.Id, 999), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.ComplementNotFound");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldDeactivateComplementAndTriggerSync()
    {
        var group = CreateGroup(companyId: 4);
        var added = group.AddComplement(500, 1m);
        _complementGroupRepository.GetByIdForUpdateAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);

        var result = await _handler.Handle(new RemoveComplementCommand(group.Id, added.Value.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        group.Complements.Single().IsActive.Should().BeFalse();
        _catalogSyncTrigger.Received(1).TriggerCompanySync(4);
    }
}
