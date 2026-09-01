using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Dining.Assignment;
using SyncBar.Application.Features.Dining.Assignment.GetActiveByDiningAreaId;
using SyncBar.Domain.Repositories;
using Xunit;
using DiningAreaAssignment = SyncBar.Domain.Entities.DiningAreaAssignment;

namespace SyncBar.Tests.Application.Features.Dining.Assignment.GetActiveByDiningAreaId;

public sealed class GetActiveAssignmentsByDiningAreaIdQueryHandlerTests
{
    private readonly IDiningAreaAssignmentRepository _assignmentRepository = Substitute.For<IDiningAreaAssignmentRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly GetActiveAssignmentsByDiningAreaIdQueryHandler _handler;

    public GetActiveAssignmentsByDiningAreaIdQueryHandlerTests()
    {
        _handler = new GetActiveAssignmentsByDiningAreaIdQueryHandler(_assignmentRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NoActiveAssignments_ShouldReturnEmptyCollection()
    {
        var query = new GetActiveAssignmentsByDiningAreaIdQuery(DiningAreaId: 1);
        _assignmentRepository.GetActiveByDiningAreaIdAsync(query.DiningAreaId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _assignmentRepository.DidNotReceive().GetActiveByEmployeeIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ActiveAssignmentsFound_ShouldMapAllFields()
    {
        var query = new GetActiveAssignmentsByDiningAreaIdQuery(DiningAreaId: 1);
        var assignment = DiningAreaAssignment.Create(query.DiningAreaId, employeeId: 10, startAt: DateTime.Now).Value;
        _assignmentRepository.GetActiveByDiningAreaIdAsync(query.DiningAreaId, Arg.Any<CancellationToken>()).Returns([assignment]);

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
