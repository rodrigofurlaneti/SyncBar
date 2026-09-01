using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Employees.Dismiss;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Employees.Dismiss;

// DismissEmployeeCommandHandler implementa ICommandHandler<DismissEmployeeCommand> diretamente
// (não herda BaseCommandHandler), então NÃO usa ILogTrackerRepository nem ExecuteWithLogAsync —
// aqui mockamos apenas IEmployeeRepository e IUnitOfWork, e o commit (quando ocorre) acontece
// uma única vez, explicitamente no próprio handler.
public sealed class DismissEmployeeCommandHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DismissEmployeeCommandHandler _handler;

    public DismissEmployeeCommandHandlerTests()
    {
        _handler = new DismissEmployeeCommandHandler(_employeeRepository, _unitOfWork);
    }

    private static Employee CreateActiveEmployee()
        => Employee.Create(
            branchId: 1, jobTitleId: 1, name: "Funcionario Teste", cpf: "12345678900",
            email: null, phone: null, hiredAt: DateTime.Now, dismissedAt: null, salary: null).Value;

    [Fact]
    public async Task Handle_EmployeeNotFound_ShouldReturnFailureWithoutCommitting()
    {
        var command = new DismissEmployeeCommand(EmployeeId: 1);
        _employeeRepository.GetByIdForUpdateAsync(command.EmployeeId, Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.NotFound");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmployeeAlreadyDismissed_ShouldReturnFailureWithoutCommitting()
    {
        var employee = CreateActiveEmployee();
        employee.Dismiss(); // já demitido antes deste Handle
        var command = new DismissEmployeeCommand(EmployeeId: 1);
        _employeeRepository.GetByIdForUpdateAsync(command.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(employee);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.AlreadyDismissed");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldDismissEmployeeAndCommit()
    {
        var employee = CreateActiveEmployee();
        var command = new DismissEmployeeCommand(EmployeeId: 1);
        _employeeRepository.GetByIdForUpdateAsync(command.EmployeeId, Arg.Any<CancellationToken>())
            .Returns(employee);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.DismissedAt.Should().NotBeNull();
        employee.IsActive.Should().BeFalse();
        // Handler sem base compartilhada: apenas o commit explícito próprio.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
