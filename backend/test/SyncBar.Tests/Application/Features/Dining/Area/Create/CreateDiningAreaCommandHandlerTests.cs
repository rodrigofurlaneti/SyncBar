using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Dining.Area.Create;
using SyncBar.Domain.Repositories;
using Xunit;
using DiningArea = SyncBar.Domain.Entities.DiningArea;

namespace SyncBar.Tests.Application.Features.Dining.Area.Create;

public sealed class CreateDiningAreaCommandHandlerTests
{
    private readonly IDiningAreaRepository _diningAreaRepository = Substitute.For<IDiningAreaRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateDiningAreaCommandHandler _handler;

    public CreateDiningAreaCommandHandlerTests()
    {
        _handler = new CreateDiningAreaCommandHandler(_diningAreaRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_InvalidBranchId_ShouldReturnFailureWithoutPersisting()
    {
        var command = new CreateDiningAreaCommand(BranchId: 0, Name: "Área Externa");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningArea.InvalidBranchId");
        await _diningAreaRepository.DidNotReceive().AddAsync(Arg.Any<DiningArea>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyName_ShouldReturnFailureWithoutPersisting()
    {
        var command = new CreateDiningAreaCommand(BranchId: 1, Name: "");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningArea.EmptyName");
        await _diningAreaRepository.DidNotReceive().AddAsync(Arg.Any<DiningArea>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistDiningAreaAndReturnItsId()
    {
        var command = new CreateDiningAreaCommand(BranchId: 1, Name: "Área Externa");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Id é sempre 0 em teste: a fábrica pública não expõe forma de setá-lo.
        result.Value.Should().Be(0);

        await _diningAreaRepository.Received(1).AddAsync(
            Arg.Is<DiningArea>(d => d.BranchId == command.BranchId && d.Name == command.Name),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
