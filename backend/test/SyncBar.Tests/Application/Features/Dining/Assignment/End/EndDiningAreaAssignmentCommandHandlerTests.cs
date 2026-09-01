using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Dining.Assignment.End;
using SyncBar.Domain.Repositories;
using Xunit;
using DiningAreaAssignment = SyncBar.Domain.Entities.DiningAreaAssignment;

namespace SyncBar.Tests.Application.Features.Dining.Assignment.End;

public sealed class EndDiningAreaAssignmentCommandHandlerTests
{
    private readonly IDiningAreaAssignmentRepository _assignmentRepository = Substitute.For<IDiningAreaAssignmentRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly EndDiningAreaAssignmentCommandHandler _handler;

    public EndDiningAreaAssignmentCommandHandlerTests()
    {
        _handler = new EndDiningAreaAssignmentCommandHandler(_assignmentRepository, _logRepository, _unitOfWork);
    }

    private static DiningAreaAssignment CreateAssignment()
        => DiningAreaAssignment.Create(diningAreaId: 1, employeeId: 10, startAt: DateTime.Now.AddHours(-2)).Value;

    [Fact]
    public async Task Handle_AssignmentNotFound_ShouldReturnFailureWithoutUpdating()
    {
        var command = new EndDiningAreaAssignmentCommand(Id: 1, EndAt: DateTime.Now);
        _assignmentRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((DiningAreaAssignment?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningAreaAssignment.NotFound");
        await _assignmentRepository.DidNotReceive().UpdateAsync(Arg.Any<DiningAreaAssignment>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AssignmentFound_ShouldSetEndAt()
    {
        var assignment = CreateAssignment();
        var command = new EndDiningAreaAssignmentCommand(Id: 1, EndAt: DateTime.Now);
        _assignmentRepository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns(assignment);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        assignment.EndAt.Should().Be(command.EndAt);
        await _assignmentRepository.Received(1).UpdateAsync(assignment, Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
