using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Dining.Table.Update;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Dining.Table.Update;

public sealed class UpdateDiningAreaTableCommandHandlerTests
{
    private readonly IDiningAreaTableRepository _repository = Substitute.For<IDiningAreaTableRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateDiningAreaTableCommandHandler _handler;

    public UpdateDiningAreaTableCommandHandlerTests()
    {
        _handler = new UpdateDiningAreaTableCommandHandler(_repository, _logRepository, _unitOfWork);
    }

    private static UpdateDiningAreaTableCommand CreateValidCommand(long id = 1, long diningAreaId = 2, long diningTableId = 3)
        => new(id, diningAreaId, diningTableId);

    private static DiningAreaTable CreateExistingDiningAreaTable(long diningAreaId = 2, long diningTableId = 3)
        => DiningAreaTable.Create(diningAreaId, diningTableId).Value;

    [Fact]
    public async Task Handle_DiningAreaTableNotFound_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand(id: 99);
        _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((DiningAreaTable?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningAreaTable.NotFound");
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<DiningAreaTable>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewTableAlreadyAssignedToAnotherArea_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand(id: 5, diningAreaId: 2, diningTableId: 9);
        var entity = CreateExistingDiningAreaTable(diningAreaId: 2, diningTableId: 3);
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(entity);
        // Trocando a mesa (3 -> 9): o handler checa se a mesa nova já está em uso.
        _repository.ExistsByTableIdAsync(9, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningAreaTable.AlreadyAssigned");
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<DiningAreaTable>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameTableId_ShouldSkipDuplicateCheckAndUpdateArea()
    {
        // Mantendo a mesma mesa (só trocando a área), o handler não deve nem consultar
        // ExistsByTableIdAsync — a checagem só roda quando o DiningTableId muda.
        var command = CreateValidCommand(id: 5, diningAreaId: 8, diningTableId: 3);
        var entity = CreateExistingDiningAreaTable(diningAreaId: 2, diningTableId: 3);
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(entity);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.DidNotReceive().ExistsByTableIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        entity.DiningAreaId.Should().Be(8);
        entity.DiningTableId.Should().Be(3);
        await _repository.Received(1).UpdateAsync(entity, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequestWithNewFreeTable_ShouldUpdateAssignmentAndPersist()
    {
        var command = CreateValidCommand(id: 5, diningAreaId: 2, diningTableId: 9);
        var entity = CreateExistingDiningAreaTable(diningAreaId: 2, diningTableId: 3);
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(entity);
        _repository.ExistsByTableIdAsync(9, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        entity.DiningTableId.Should().Be(9);
        await _repository.Received(1).UpdateAsync(entity, Arg.Any<CancellationToken>());
        // 1 commit explícito do handler + 1 commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
