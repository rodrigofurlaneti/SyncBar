using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Dining.Assignment.Create;
using SyncBar.Domain.Repositories;
using Xunit;
using DiningAreaAssignment = SyncBar.Domain.Entities.DiningAreaAssignment;

namespace SyncBar.Tests.Application.Features.Dining.Assignment.Create;

public sealed class CreateDiningAreaAssignmentCommandHandlerTests
{
    private readonly IDiningAreaAssignmentRepository _assignmentRepository = Substitute.For<IDiningAreaAssignmentRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateDiningAreaAssignmentCommandHandler _handler;

    public CreateDiningAreaAssignmentCommandHandlerTests()
    {
        _handler = new CreateDiningAreaAssignmentCommandHandler(_assignmentRepository, _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_InvalidDiningAreaId_ShouldReturnFailureWithoutPersisting()
    {
        var command = new CreateDiningAreaAssignmentCommand(DiningAreaId: 0, EmployeeId: 1, StartAt: DateTime.Now);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningAreaAssignment.InvalidDiningAreaId");
        await _assignmentRepository.DidNotReceive().AddAsync(Arg.Any<DiningAreaAssignment>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidEmployeeId_ShouldReturnFailureWithoutPersisting()
    {
        var command = new CreateDiningAreaAssignmentCommand(DiningAreaId: 1, EmployeeId: 0, StartAt: DateTime.Now);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningAreaAssignment.InvalidEmployeeId");
        await _assignmentRepository.DidNotReceive().AddAsync(Arg.Any<DiningAreaAssignment>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidStartAt_ShouldReturnFailureWithoutPersisting()
    {
        var command = new CreateDiningAreaAssignmentCommand(DiningAreaId: 1, EmployeeId: 1, StartAt: default);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DiningAreaAssignment.InvalidStartAt");
        await _assignmentRepository.DidNotReceive().AddAsync(Arg.Any<DiningAreaAssignment>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistAssignmentAndReturnItsId()
    {
        var command = new CreateDiningAreaAssignmentCommand(DiningAreaId: 1, EmployeeId: 10, StartAt: DateTime.Now);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Id é sempre 0 em teste: a fábrica pública não expõe forma de setá-lo.
        result.Value.Should().Be(0);

        await _assignmentRepository.Received(1).AddAsync(
            Arg.Is<DiningAreaAssignment>(a => a.DiningAreaId == command.DiningAreaId && a.EmployeeId == command.EmployeeId && a.StartAt == command.StartAt),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
