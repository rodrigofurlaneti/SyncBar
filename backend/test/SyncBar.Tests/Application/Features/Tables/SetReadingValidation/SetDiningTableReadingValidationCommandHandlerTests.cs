using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Tables.SetReadingValidation;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Tables.SetReadingValidation;

public sealed class SetDiningTableReadingValidationCommandHandlerTests
{
    private readonly IDiningTableRepository _diningTableRepository = Substitute.For<IDiningTableRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SetDiningTableReadingValidationCommandHandler _handler;

    public SetDiningTableReadingValidationCommandHandlerTests()
    {
        _handler = new SetDiningTableReadingValidationCommandHandler(_diningTableRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_TableNotFound_ShouldReturnFailure()
    {
        var command = new SetDiningTableReadingValidationCommand(1, true, true, true);
        _diningTableRepository.GetByIdForUpdateAsync(command.DiningTableId, Arg.Any<CancellationToken>())
            .Returns((DiningTable?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningTable.NotFound");
    }

    [Fact]
    public async Task Handle_TableInactive_ShouldReturnFailure()
    {
        var table = DiningTable.Create(branchId: 1, tableStatusId: 1, number: 5, capacity: 4).Value;
        table.Deactivate();
        var command = new SetDiningTableReadingValidationCommand(table.Id, true, false, true);
        _diningTableRepository.GetByIdForUpdateAsync(command.DiningTableId, Arg.Any<CancellationToken>())
            .Returns(table);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningTable.NotFound");
    }

    [Fact]
    public async Task Handle_ValidTable_ShouldUpdateAllThreeFlagsAndCommit()
    {
        var table = DiningTable.Create(branchId: 1, tableStatusId: 1, number: 5, capacity: 4).Value;
        var command = new SetDiningTableReadingValidationCommand(
            table.Id, IsCameraInputEnabled: true, IsBarcodeEnabled: true, IsQrCodeEnabled: false);
        _diningTableRepository.GetByIdForUpdateAsync(command.DiningTableId, Arg.Any<CancellationToken>())
            .Returns(table);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        table.IsCameraInputEnabled.Should().BeTrue();
        table.IsBarcodeEnabled.Should().BeTrue();
        table.IsQrCodeEnabled.Should().BeFalse();
        // 1 commit explícito do handler + 1 do finally da base (persistência do log).
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
