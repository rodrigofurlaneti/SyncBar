using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Dining.Assignment.Deactivate;
using SyncBar.Domain.Repositories;
using Xunit;
using DiningAreaAssignment = SyncBar.Domain.Entities.DiningAreaAssignment;

namespace SyncBar.Tests.Application.Features.Dining.Assignment.Deactivate;

public sealed class DeactivateDiningAreaAssignmentCommandHandlerTests
{
    private readonly IDiningAreaAssignmentRepository _assignmentRepository = Substitute.For<IDiningAreaAssignmentRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DeactivateDiningAreaAssignmentCommandHandler _handler;

    public DeactivateDiningAreaAssignmentCommandHandlerTests()
    {
        _handler = new DeactivateDiningAreaAssignmentCommandHandler(_assignmentRepository, _logRepository, _unitOfWork);
    }

    private static DiningAreaAssignment CreateAssignment()
        => DiningAreaAssignment.Create(diningAreaId: 1, employeeId: 10, startAt: DateTime.Now).Value;

    [Fact]
    public async Task Handle_AssignmentNotFound_ShouldReturnFailureWithoutUpdating()
    {
        var command = new DeactivateDiningAreaAssignmentCommand(Id: 1);
        _assignmentRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((DiningAreaAssignment?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningAreaAssignment.NotFound");
        await _assignmentRepository.DidNotReceive().UpdateAsync(Arg.Any<DiningAreaAssignment>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AssignmentFound_ShouldDeactivateIt()
    {
        var assignment = CreateAssignment();
        var command = new DeactivateDiningAreaAssignmentCommand(Id: 1);
        _assignmentRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(assignment);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        assignment.IsActive.Should().BeFalse();
        await _assignmentRepository.Received(1).UpdateAsync(assignment, Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
