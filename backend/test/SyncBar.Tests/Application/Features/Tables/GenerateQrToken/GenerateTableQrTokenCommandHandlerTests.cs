using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Tables.GenerateQrToken;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Tables.GenerateQrToken;

public sealed class GenerateTableQrTokenCommandHandlerTests
{
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GenerateTableQrTokenCommandHandler _handler;

    public GenerateTableQrTokenCommandHandlerTests()
    {
        _handler = new GenerateTableQrTokenCommandHandler(_diningTableRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_TableNotFound_ShouldReturnFailureWithoutCommittingExplicitly()
    {
        var command = new GenerateTableQrTokenCommand(DiningTableId: 1);
        _diningTableRepository.GetByIdForUpdateAsync(command.DiningTableId, Arg.Any<CancellationToken>())
            .Returns((DiningTable?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningTable.NotFound");
        // Sem commit explícito nesse ramo: só o commit do finally da base (persistência do log).
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TableInactive_ShouldReturnFailure()
    {
        var table = DiningTable.Create(branchId: 1, tableStatusId: 1, number: 5, capacity: 4).Value;
        table.Deactivate();
        var command = new GenerateTableQrTokenCommand(table.Id);
        _diningTableRepository.GetByIdForUpdateAsync(command.DiningTableId, Arg.Any<CancellationToken>())
            .Returns(table);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningTable.NotFound");
    }

    [Fact]
    public async Task Handle_ValidTable_ShouldGenerateNewTokenPersistItAndCommit()
    {
        var table = DiningTable.Create(branchId: 1, tableStatusId: 1, number: 5, capacity: 4).Value;
        table.QrToken.Should().BeNull();
        var command = new GenerateTableQrTokenCommand(table.Id);
        _diningTableRepository.GetByIdForUpdateAsync(command.DiningTableId, Arg.Any<CancellationToken>())
            .Returns(table);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        table.QrToken.Should().Be(result.Value);
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CalledTwice_ShouldOverwritePreviousTokenWithANewOne()
    {
        var table = DiningTable.Create(branchId: 1, tableStatusId: 1, number: 5, capacity: 4).Value;
        var command = new GenerateTableQrTokenCommand(table.Id);
        _diningTableRepository.GetByIdForUpdateAsync(command.DiningTableId, Arg.Any<CancellationToken>())
            .Returns(table);

        var firstResult = await _handler.Handle(command, CancellationToken.None);
        var secondResult = await _handler.Handle(command, CancellationToken.None);

        firstResult.IsSuccess.Should().BeTrue();
        secondResult.IsSuccess.Should().BeTrue();
        secondResult.Value.Should().NotBe(firstResult.Value);
        table.QrToken.Should().Be(secondResult.Value);
    }
}
