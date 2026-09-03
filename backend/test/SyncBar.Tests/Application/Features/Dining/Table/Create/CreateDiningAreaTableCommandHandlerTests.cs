using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Dining.Table.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Dining.Table.Create;

public sealed class CreateDiningAreaTableCommandHandlerTests
{
    private readonly IDiningAreaTableRepository _repository = Substitute.For<IDiningAreaTableRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateDiningAreaTableCommandHandler _handler;

    public CreateDiningAreaTableCommandHandlerTests()
    {
        _handler = new CreateDiningAreaTableCommandHandler(_repository, _logRepository, _unitOfWork);
    }

    private static CreateDiningAreaTableCommand CreateValidCommand(long diningAreaId = 1, long diningTableId = 2)
        => new(diningAreaId, diningTableId);

    [Fact]
    public async Task Handle_TableAlreadyAssignedToArea_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand();
        _repository.ExistsByTableIdAsync(command.DiningTableId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningAreaTable.AlreadyAssigned");
        await _repository.DidNotReceive().AddAsync(Arg.Any<DiningAreaTable>(), Arg.Any<CancellationToken>());
        // Sem persistência: só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidDiningAreaId_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand(diningAreaId: 0, diningTableId: 5);
        _repository.ExistsByTableIdAsync(command.DiningTableId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningAreaTable.InvalidDiningAreaId");
        await _repository.DidNotReceive().AddAsync(Arg.Any<DiningAreaTable>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistDiningAreaTableAndReturnSuccess()
    {
        var command = CreateValidCommand(diningAreaId: 3, diningTableId: 7);
        _repository.ExistsByTableIdAsync(command.DiningTableId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Id é sempre 0 em teste: a fábrica pública não expõe forma de setá-lo.
        result.Value.Should().Be(0);

        await _repository.Received(1).AddAsync(
            Arg.Is<DiningAreaTable>(e => e.DiningAreaId == 3 && e.DiningTableId == 7 && e.IsActive),
            Arg.Any<CancellationToken>());
        // 1 commit explícito do handler + 1 commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
