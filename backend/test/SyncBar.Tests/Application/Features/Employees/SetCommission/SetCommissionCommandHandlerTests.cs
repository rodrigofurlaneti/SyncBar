using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Employees.SetCommission;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Employees.SetCommission;

public sealed class SetCommissionCommandHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SetCommissionCommandHandler _handler;

    public SetCommissionCommandHandlerTests()
    {
        _handler = new SetCommissionCommandHandler(_employeeRepository, _logRepository, _unitOfWork);
    }

    private static Employee CreateActiveEmployee()
        => Employee.Create(
            branchId: 1, jobTitleId: 1, name: "Funcionario Teste", cpf: "12345678900",
            email: null, phone: null, hiredAt: DateTime.Now, dismissedAt: null, salary: null).Value;

    [Fact]
    public async Task Handle_EmployeeNotFound_ShouldReturnFailureWithoutCommittingExplicitly()
    {
        var command = new SetCommissionCommand(EmployeeId: 1, CommissionPercent: 10m);
        _employeeRepository.GetByIdForUpdateAsync(command.EmployeeId, Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmployeeInactive_ShouldReturnFailureWithoutCommittingExplicitly()
    {
        var employee = CreateActiveEmployee();
        employee.Deactivate();
        var command = new SetCommissionCommand(EmployeeId: 1, CommissionPercent: 10m);
        _employeeRepository.GetByIdForUpdateAsync(command.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(employee);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CommissionBelowZero_ShouldReturnFailureWithoutCommittingExplicitly()
    {
        var employee = CreateActiveEmployee();
        var command = new SetCommissionCommand(EmployeeId: 1, CommissionPercent: -5m);
        _employeeRepository.GetByIdForUpdateAsync(command.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(employee);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.InvalidCommission");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CommissionAboveHundred_ShouldReturnFailureWithoutCommittingExplicitly()
    {
        var employee = CreateActiveEmployee();
        var command = new SetCommissionCommand(EmployeeId: 1, CommissionPercent: 100.01m);
        _employeeRepository.GetByIdForUpdateAsync(command.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(employee);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.InvalidCommission");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommission_ShouldSetCommissionAndCommit()
    {
        var employee = CreateActiveEmployee();
        var command = new SetCommissionCommand(EmployeeId: 1, CommissionPercent: 35.5m);
        _employeeRepository.GetByIdForUpdateAsync(command.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(employee);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.CommissionPercent.Should().Be(command.CommissionPercent);
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LowerBoundaryCommissionZero_ShouldSucceed()
    {
        var employee = CreateActiveEmployee();
        var command = new SetCommissionCommand(EmployeeId: 1, CommissionPercent: 0m);
        _employeeRepository.GetByIdForUpdateAsync(command.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(employee);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.CommissionPercent.Should().Be(0m);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpperBoundaryCommissionHundred_ShouldSucceed()
    {
        var employee = CreateActiveEmployee();
        var command = new SetCommissionCommand(EmployeeId: 1, CommissionPercent: 100m);
        _employeeRepository.GetByIdForUpdateAsync(command.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(employee);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.CommissionPercent.Should().Be(100m);
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NullCommission_ShouldClearCommissionAndCommit()
    {
        var employee = CreateActiveEmployee();
        employee.SetCommissionPercent(50m);
        var command = new SetCommissionCommand(EmployeeId: 1, CommissionPercent: null);
        _employeeRepository.GetByIdForUpdateAsync(command.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(employee);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.CommissionPercent.Should().BeNull();
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
