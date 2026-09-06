using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Catalog.Complements.CreateComplementGroup;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Catalog.Complements.CreateComplementGroup;

public sealed class CreateComplementGroupCommandHandlerTests
{
    private readonly IComplementGroupRepository _complementGroupRepository = Substitute.For<IComplementGroupRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateComplementGroupCommandHandler _handler;

    public CreateComplementGroupCommandHandlerTests()
    {
        _handler = new CreateComplementGroupCommandHandler(_complementGroupRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_EmptyName_ShouldReturnFailure()
    {
        var result = await _handler.Handle(
            new CreateComplementGroupCommand(1, "", ComplementGroupTypeIds.SelecaoAdicional, 0, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.EmptyName");
    }

    [Fact]
    public async Task Handle_MinGreaterThanMax_ShouldReturnFailure()
    {
        var result = await _handler.Handle(
            new CreateComplementGroupCommand(1, "Bebidas", ComplementGroupTypeIds.SelecaoAdicional, 2, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ComplementGroup.MinGreaterThanMax");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldPersistWithoutTriggeringSync()
    {
        var result = await _handler.Handle(
            new CreateComplementGroupCommand(3, "Bebidas", ComplementGroupTypeIds.SelecaoAdicional, 0, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _complementGroupRepository.Received(1).AddAsync(
            Arg.Is<ComplementGroup>(g => g.Name == "Bebidas" && g.CompanyId == 3), Arg.Any<CancellationToken>());
    }
}
