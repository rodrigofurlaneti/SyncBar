using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Shift.OpenShift;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Shift.OpenShift;

public sealed class OpenShiftClosingCommandHandlerTests
{
    private readonly IShiftClosingRepository _shiftClosingRepository = Substitute.For<IShiftClosingRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly OpenShiftClosingCommandHandler _handler;

    public OpenShiftClosingCommandHandlerTests()
    {
        _handler = new OpenShiftClosingCommandHandler(_shiftClosingRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_BranchAlreadyHasOpenShift_ShouldReturnFailureWithoutPersistingNewShift()
    {
        var command = new OpenShiftClosingCommand(BranchId: 1, OpenedByEmployeeId: 10);
        var existingOpenShift = ShiftClosing.Open(command.BranchId, 99).Value;
        _shiftClosingRepository.GetOpenByBranchAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns(existingOpenShift);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ShiftClosing.AlreadyOpen");

        await _shiftClosingRepository.DidNotReceive().AddAsync(Arg.Any<ShiftClosing>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidBranchId_ShouldReturnFailureWithoutPersisting()
    {
        var command = new OpenShiftClosingCommand(BranchId: 0, OpenedByEmployeeId: 10);
        _shiftClosingRepository.GetOpenByBranchAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns((ShiftClosing?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ShiftClosing.InvalidBranch");

        await _shiftClosingRepository.DidNotReceive().AddAsync(Arg.Any<ShiftClosing>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistNewShiftAndReturnItsId()
    {
        var command = new OpenShiftClosingCommand(BranchId: 1, OpenedByEmployeeId: 10);
        _shiftClosingRepository.GetOpenByBranchAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns((ShiftClosing?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Id é sempre 0 em teste: a fábrica pública não expõe forma de setá-lo.
        result.Value.Should().Be(0);

        await _shiftClosingRepository.Received(1).AddAsync(
            Arg.Is<ShiftClosing>(s =>
                s.BranchId == command.BranchId &&
                s.OpenedByEmployeeId == command.OpenedByEmployeeId &&
                s.IsActive &&
                s.IsOpen()),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
