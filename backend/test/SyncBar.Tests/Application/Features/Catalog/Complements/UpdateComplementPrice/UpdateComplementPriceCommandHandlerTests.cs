using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Features.Catalog.Complements.UpdateComplementPrice;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.Complements.UpdateComplementPrice;

public sealed class UpdateComplementPriceCommandHandlerTests
{
    private readonly IComplementGroupRepository _complementGroupRepository = Substitute.For<IComplementGroupRepository>();
    private readonly IIfoodCatalogSyncTrigger _catalogSyncTrigger = Substitute.For<IIfoodCatalogSyncTrigger>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateComplementPriceCommandHandler _handler;

    public UpdateComplementPriceCommandHandlerTests()
    {
        _handler = new UpdateComplementPriceCommandHandler(_complementGroupRepository, _catalogSyncTrigger, _logRepository, _unitOfWork);
    }

    private static void SetId(Entity entity, long id)
        => typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);

    private static ComplementGroup CreateGroup(long id = 1, long companyId = 1)
    {
        var group = ComplementGroup.Create(companyId, "Bebidas", ComplementGroupTypeIds.SelecaoAdicional, 0, 1).Value;
        SetId(group, id);
        return group;
    }

    [Fact]
    public async Task Handle_GroupNotFound_ShouldReturnFailure()
    {
        _complementGroupRepository.GetByIdForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns((ComplementGroup?)null);

        var result = await _handler.Handle(new UpdateComplementPriceCommand(1, 5, 3m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.NotFound");
    }

    [Fact]
    public async Task Handle_ComplementNotFound_ShouldReturnFailure()
    {
        var group = CreateGroup();
        _complementGroupRepository.GetByIdForUpdateAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);

        var result = await _handler.Handle(new UpdateComplementPriceCommand(group.Id, 999, 3m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.ComplementNotFound");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldUpdatePriceAndTriggerSync()
    {
        var group = CreateGroup(companyId: 6);
        var added = group.AddComplement(500, 1m);
        _complementGroupRepository.GetByIdForUpdateAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);

        var result = await _handler.Handle(new UpdateComplementPriceCommand(group.Id, added.Value.Id, 5m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        group.Complements.Single().ExtraPrice.Should().Be(5m);
        _catalogSyncTrigger.Received(1).TriggerCompanySync(6);
    }
}
