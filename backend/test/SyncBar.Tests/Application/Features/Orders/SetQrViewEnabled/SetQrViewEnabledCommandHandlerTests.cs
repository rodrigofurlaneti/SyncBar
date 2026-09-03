using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.SetQrViewEnabled;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.SetQrViewEnabled;

public sealed class SetQrViewEnabledCommandHandlerTests
{
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly SetQrViewEnabledCommandHandler _handler;

    public SetQrViewEnabledCommandHandlerTests()
    {
        _handler = new SetQrViewEnabledCommandHandler(_diningTableRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoTablesInBranch_ReturnsSuccessWithoutUpdatingOrCommitting()
    {
        var command = new SetQrViewEnabledCommand(BranchId: 1, Enabled: false);
        _diningTableRepository.GetByBranchAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<DiningTable>)Array.Empty<DiningTable>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _diningTableRepository.DidNotReceive().Update(Arg.Any<DiningTable>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TablesExist_DisablesQrViewOnAllTablesAndCommitsOnce()
    {
        var table1 = DiningTable.Create(1, 1, 1, null).Value;
        var table2 = DiningTable.Create(1, 1, 2, null).Value;
        var command = new SetQrViewEnabledCommand(BranchId: 1, Enabled: false);
        _diningTableRepository.GetByBranchAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<DiningTable>)new List<DiningTable> { table1, table2 });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        table1.IsQrViewEnabled.Should().BeFalse();
        table2.IsQrViewEnabled.Should().BeFalse();
        _diningTableRepository.Received(1).Update(table1);
        _diningTableRepository.Received(1).Update(table2);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TablesExist_EnablesQrViewOnAllTablesAndCommitsOnce()
    {
        var table1 = DiningTable.Create(1, 1, 1, null).Value;
        table1.SetQrViewEnabled(false);
        var command = new SetQrViewEnabledCommand(BranchId: 1, Enabled: true);
        _diningTableRepository.GetByBranchAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<DiningTable>)new List<DiningTable> { table1 });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        table1.IsQrViewEnabled.Should().BeTrue();
        _diningTableRepository.Received(1).Update(table1);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
