using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Employees.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Employees.Create;

public sealed class CreateEmployeeCommandHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly IJobTitleRepository _jobTitleRepository = Substitute.For<IJobTitleRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateEmployeeCommandHandler _handler;

    public CreateEmployeeCommandHandlerTests()
    {
        _handler = new CreateEmployeeCommandHandler(_employeeRepository, _jobTitleRepository, _logRepository, _unitOfWork);
    }

    private static JobTitle CreateActiveJobTitle(long companyId = 1, string name = "Garçom")
        => JobTitle.Create(companyId, name).Value;

    private static CreateEmployeeCommand CreateValidCommand(string name = "Funcionario Teste", string cpf = "12345678900")
        => new(BranchId: 1, JobTitleId: 1, Name: name, Cpf: cpf, Email: "func@teste.com", Phone: "11999990000", HiredAt: DateTime.Now, Salary: 1500m);

    [Fact]
    public async Task Handle_CpfAlreadyExists_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand();
        _employeeRepository.ExistsByCpfAsync(command.Cpf, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.CpfAlreadyExists");
        await _employeeRepository.DidNotReceive().AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
        // Sem persistência: só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_JobTitleNotFound_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand();
        _employeeRepository.ExistsByCpfAsync(command.Cpf, Arg.Any<CancellationToken>()).Returns(false);
        _jobTitleRepository.GetByIdAsync(command.JobTitleId, Arg.Any<CancellationToken>()).Returns((JobTitle?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JobTitle.NotFound");
        await _employeeRepository.DidNotReceive().AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_JobTitleInactive_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand();
        var jobTitle = CreateActiveJobTitle();
        jobTitle.Deactivate();
        _employeeRepository.ExistsByCpfAsync(command.Cpf, Arg.Any<CancellationToken>()).Returns(false);
        _jobTitleRepository.GetByIdAsync(command.JobTitleId, Arg.Any<CancellationToken>()).Returns(jobTitle);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JobTitle.NotFound");
        await _employeeRepository.DidNotReceive().AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyName_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand(name: "");
        var jobTitle = CreateActiveJobTitle();
        _employeeRepository.ExistsByCpfAsync(command.Cpf, Arg.Any<CancellationToken>()).Returns(false);
        _jobTitleRepository.GetByIdAsync(command.JobTitleId, Arg.Any<CancellationToken>()).Returns(jobTitle);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.EmptyName");
        await _employeeRepository.DidNotReceive().AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldPersistEmployeeAndReturnItsId()
    {
        var command = CreateValidCommand();
        var jobTitle = CreateActiveJobTitle();
        _employeeRepository.ExistsByCpfAsync(command.Cpf, Arg.Any<CancellationToken>()).Returns(false);
        _jobTitleRepository.GetByIdAsync(command.JobTitleId, Arg.Any<CancellationToken>()).Returns(jobTitle);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Id é sempre 0 em teste: a fábrica pública não expõe forma de setá-lo.
        result.Value.Should().Be(0);

        await _employeeRepository.Received(1).AddAsync(
            Arg.Is<Employee>(e =>
                e.BranchId == command.BranchId &&
                e.JobTitleId == command.JobTitleId &&
                e.Name == command.Name &&
                e.Cpf == command.Cpf &&
                e.Email == command.Email &&
                e.Phone == command.Phone &&
                e.HiredAt == command.HiredAt &&
                e.DismissedAt == null &&
                e.Salary == command.Salary &&
                e.IsActive),
            Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
