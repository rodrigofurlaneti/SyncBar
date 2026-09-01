using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Dining.Assignment;
using SyncBar.Application.Features.Dining.Assignment.GetActiveByEmployeeId;
using SyncBar.Domain.Repositories;
using Xunit;
using DiningAreaAssignment = SyncBar.Domain.Entities.DiningAreaAssignment;

namespace SyncBar.Tests.Application.Features.Dining.Assignment.GetActiveByEmployeeId;

public sealed class GetActiveAssignmentsByEmployeeIdQueryHandlerTests
{
    private readonly IDiningAreaAssignmentRepository _assignmentRepository = Substitute.For<IDiningAreaAssignmentRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetActiveAssignmentsByEmployeeIdQueryHandler _handler;

    public GetActiveAssignmentsByEmployeeIdQueryHandlerTests()
    {
        _handler = new GetActiveAssignmentsByEmployeeIdQueryHandler(_assignmentRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoActiveAssignments_ShouldReturnEmptyCollection()
    {
        var query = new GetActiveAssignmentsByEmployeeIdQuery(EmployeeId: 10);
        _assignmentRepository.GetActiveByEmployeeIdAsync(query.EmployeeId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _assignmentRepository.DidNotReceive().GetActiveByDiningAreaIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ActiveAssignmentsFound_ShouldMapAllFields()
    {
        var query = new GetActiveAssignmentsByEmployeeIdQuery(EmployeeId: 10);
        var assignment = DiningAreaAssignment.Create(diningAreaId: 1, query.EmployeeId, startAt: DateTime.Now).Value;
        _assignmentRepository.GetActiveByEmployeeIdAsync(query.EmployeeId, Arg.Any<CancellationToken>()).Returns([assignment]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var response = result.Value.Single();
        response.Id.Should().Be(assignment.Id);
        response.DiningAreaId.Should().Be(assignment.DiningAreaId);
        response.EmployeeId.Should().Be(assignment.EmployeeId);
        response.StartAt.Should().Be(assignment.StartAt);
    }
}
