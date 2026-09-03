using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Dining.Table.Deactivate;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Dining.Table.Deactivate;

public sealed class DeactivateDiningAreaTableCommandHandlerTests
{
    private readonly IDiningAreaTableRepository _repository = Substitute.For<IDiningAreaTableRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeactivateDiningAreaTableCommandHandler _handler;

    public DeactivateDiningAreaTableCommandHandlerTests()
    {
        _handler = new DeactivateDiningAreaTableCommandHandler(_repository, _logRepository, _unitOfWork);
    }

    private static DiningAreaTable CreateActiveDiningAreaTable(long diningAreaId = 1, long diningTableId = 2)
        => DiningAreaTable.Create(diningAreaId, diningTableId).Value;

    [Fact]
    public async Task Handle_DiningAreaTableNotFound_ShouldReturnFailureWithoutPersisting()
    {
        var command = new DeactivateDiningAreaTableCommand(99);
        _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((DiningAreaTable?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningAreaTable.NotFound");
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<DiningAreaTable>(), Arg.Any<CancellationToken>());
        // Sem persistência: só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingDiningAreaTable_ShouldDeactivateAndPersist()
    {
        var command = new DeactivateDiningAreaTableCommand(5);
        var entity = CreateActiveDiningAreaTable();
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(entity);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        entity.IsActive.Should().BeFalse();
        await _repository.Received(1).UpdateAsync(entity, Arg.Any<CancellationToken>());
        // 1 commit explícito do handler + 1 commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
