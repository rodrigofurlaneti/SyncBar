using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Orders.SetTableReadingValidation;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Orders.SetTableReadingValidation;

public sealed class SetTableReadingValidationCommandHandlerTests
{
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SetTableReadingValidationCommandHandler _handler;

    public SetTableReadingValidationCommandHandlerTests()
    {
        _handler = new SetTableReadingValidationCommandHandler(_diningTableRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoTablesInBranch_ShouldSucceedWithoutUpdatingAnything()
    {
        var command = new SetTableReadingValidationCommand(BranchId: 1, true, true, true);
        _diningTableRepository.GetByBranchAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DiningTable>());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _diningTableRepository.DidNotReceive().Update(Arg.Any<DiningTable>());
    }

    [Fact]
    public async Task Handle_MultipleTablesInBranch_ShouldUpdateAllOfThemAndCommit()
    {
        var command = new SetTableReadingValidationCommand(
            BranchId: 1, IsCameraInputEnabled: true, IsBarcodeEnabled: false, IsQrCodeEnabled: true);
        var table1 = DiningTable.Create(command.BranchId, 1, 1, null).Value;
        var table2 = DiningTable.Create(command.BranchId, 1, 2, null).Value;
        _diningTableRepository.GetByBranchAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns(new[] { table1, table2 });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        table1.IsCameraInputEnabled.Should().BeTrue();
        table1.IsBarcodeEnabled.Should().BeFalse();
        table1.IsQrCodeEnabled.Should().BeTrue();
        table2.IsCameraInputEnabled.Should().BeTrue();
        table2.IsBarcodeEnabled.Should().BeFalse();
        table2.IsQrCodeEnabled.Should().BeTrue();
        _diningTableRepository.Received(1).Update(table1);
        _diningTableRepository.Received(1).Update(table2);
        // 1 commit explícito do handler + 1 do finally da base (persistência do log).
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
