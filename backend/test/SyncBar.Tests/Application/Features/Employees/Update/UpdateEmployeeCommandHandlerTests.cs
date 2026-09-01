using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Employees.Update;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Employees.Update;

public sealed class UpdateEmployeeCommandHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly IJobTitleRepository _jobTitleRepository = Substitute.For<IJobTitleRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly UpdateEmployeeCommandHandler _handler;

    public UpdateEmployeeCommandHandlerTests()
    {
        _handler = new UpdateEmployeeCommandHandler(_employeeRepository, _jobTitleRepository, _logRepository, _unitOfWork);
    }

    private static Employee CreateActiveEmployee()
        => Employee.Create(
            branchId: 1, jobTitleId: 1, name: "Funcionario Teste", cpf: "12345678900",
            email: "antigo@teste.com", phone: "11988880000", hiredAt: DateTime.Now, dismissedAt: null, salary: 1200m).Value;

    private static JobTitle CreateActiveJobTitle(long companyId = 1, string name = "Garçom")
        => JobTitle.Create(companyId, name).Value;

    private static UpdateEmployeeCommand CreateValidCommand(string name = "Funcionario Atualizado")
        => new(EmployeeId: 1, JobTitleId: 2, Name: name, Email: "novo@teste.com", Phone: "11999990000", Salary: 1800m);

    [Fact]
    public async Task Handle_EmployeeNotFound_ShouldReturnFailureWithoutLookingUpJobTitle()
    {
        var command = CreateValidCommand();
        _employeeRepository.GetByIdForUpdateAsync(command.EmployeeId, Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.NotFound");
        await _jobTitleRepository.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmployeeInactive_ShouldReturnFailure()
    {
        var employee = CreateActiveEmployee();
        employee.Deactivate();
        var command = CreateValidCommand();
        _employeeRepository.GetByIdForUpdateAsync(command.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(employee);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_JobTitleNotFound_ShouldReturnFailureWithoutUpdatingEmployee()
    {
        var employee = CreateActiveEmployee();
        var command = CreateValidCommand();
        _employeeRepository.GetByIdForUpdateAsync(command.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(employee);
        _jobTitleRepository.GetByIdAsync(command.JobTitleId, Arg.Any<CancellationToken>())
            .Returns((JobTitle?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JobTitle.NotFound");
        employee.JobTitleId.Should().Be(1); // inalterado
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_JobTitleInactive_ShouldReturnFailureWithoutUpdatingEmployee()
    {
        var employee = CreateActiveEmployee();
        var jobTitle = CreateActiveJobTitle();
        jobTitle.Deactivate();
        var command = CreateValidCommand();
        _employeeRepository.GetByIdForUpdateAsync(command.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(employee);
        _jobTitleRepository.GetByIdAsync(command.JobTitleId, Arg.Any<CancellationToken>())
            .Returns(jobTitle);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JobTitle.NotFound");
        employee.JobTitleId.Should().Be(1); // inalterado
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyName_ShouldReturnFailureWithoutCommittingExplicitly()
    {
        var employee = CreateActiveEmployee();
        var jobTitle = CreateActiveJobTitle();
        var command = CreateValidCommand(name: "");
        _employeeRepository.GetByIdForUpdateAsync(command.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(employee);
        _jobTitleRepository.GetByIdAsync(command.JobTitleId, Arg.Any<CancellationToken>())
            .Returns(jobTitle);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.EmptyName");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldUpdateEmployeeDetailsAndCommit()
    {
        var employee = CreateActiveEmployee();
        var jobTitle = CreateActiveJobTitle();
        var command = CreateValidCommand();
        _employeeRepository.GetByIdForUpdateAsync(command.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(employee);
        _jobTitleRepository.GetByIdAsync(command.JobTitleId, Arg.Any<CancellationToken>())
            .Returns(jobTitle);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.JobTitleId.Should().Be(command.JobTitleId);
        employee.Name.Should().Be(command.Name);
        employee.Email.Should().Be(command.Email);
        employee.Phone.Should().Be(command.Phone);
        employee.Salary.Should().Be(command.Salary);
        employee.UpdatedAt.Should().NotBeNull();
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
