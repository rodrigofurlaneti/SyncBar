using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Features.Branches.SetSelfServiceEmployee;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Branches.SetSelfServiceEmployee;

public sealed class SetSelfServiceEmployeeCommandHandlerTests
{
    private readonly IBranchRepository _branchRepository = Substitute.For<IBranchRepository>();
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly SetSelfServiceEmployeeCommandHandler _handler;

    public SetSelfServiceEmployeeCommandHandlerTests()
    {
        _handler = new SetSelfServiceEmployeeCommandHandler(
            _branchRepository, _employeeRepository, _logRepository, _unitOfWork);
    }

    private static Branch CreateActiveBranch(long companyId = 1, string name = "Filial Centro")
        => Branch.Create(
            companyId, name, cnpj: null, phone: null,
            addressStreet: null, addressNumber: null, addressDistrict: null,
            addressCity: null, addressState: null, addressZipCode: null).Value;

    private static Employee CreateActiveEmployee(long branchId, string name = "Ana", string cpf = "11122233344")
        => Employee.Create(
            branchId, jobTitleId: 1, name, cpf, email: null, phone: null,
            hiredAt: DateTime.Now, dismissedAt: null, salary: null).Value;

    [Fact]
    public async Task Handle_BranchNotFound_ShouldReturnFailureWithoutCommittingExplicitly()
    {
        var command = new SetSelfServiceEmployeeCommand(BranchId: 1, EmployeeId: null);
        _branchRepository.GetByIdForUpdateAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns((Branch?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");

        // O handler retorna antes do commit explícito; só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BranchInactive_ShouldReturnFailure()
    {
        var branch = CreateActiveBranch();
        branch.Deactivate();
        var command = new SetSelfServiceEmployeeCommand(BranchId: 1, EmployeeId: null);
        _branchRepository.GetByIdForUpdateAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns(branch);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Branch.NotFound");

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmployeeIdInformedButNotFound_ShouldReturnFailure()
    {
        var branch = CreateActiveBranch();
        var command = new SetSelfServiceEmployeeCommand(BranchId: 1, EmployeeId: 99);
        _branchRepository.GetByIdForUpdateAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns(branch);
        _employeeRepository.GetByIdAsync(command.EmployeeId.GetValueOrDefault(), Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.NotFound");

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmployeeInactive_ShouldReturnFailure()
    {
        var branch = CreateActiveBranch();
        var employee = CreateActiveEmployee(branchId: 1);
        employee.Deactivate();
        var command = new SetSelfServiceEmployeeCommand(BranchId: 1, EmployeeId: 5);
        _branchRepository.GetByIdForUpdateAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns(branch);
        _employeeRepository.GetByIdAsync(command.EmployeeId.GetValueOrDefault(), Arg.Any<CancellationToken>())
            .Returns(employee);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.NotFound");
    }

    [Fact]
    public async Task Handle_EmployeeFromDifferentBranch_ShouldReturnFailure()
    {
        var branch = CreateActiveBranch();
        // Funcionário ativo, mas pertence a outra filial (BranchId: 2 != command.BranchId: 1).
        var employee = CreateActiveEmployee(branchId: 2);
        var command = new SetSelfServiceEmployeeCommand(BranchId: 1, EmployeeId: 5);
        _branchRepository.GetByIdForUpdateAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns(branch);
        _employeeRepository.GetByIdAsync(command.EmployeeId.GetValueOrDefault(), Arg.Any<CancellationToken>())
            .Returns(employee);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.NotFound");
    }

    [Fact]
    public async Task Handle_EmployeeIdNull_ShouldUnsetSelfServiceEmployeeWithoutLookingUpEmployee()
    {
        var branch = CreateActiveBranch();
        var command = new SetSelfServiceEmployeeCommand(BranchId: 1, EmployeeId: null);
        _branchRepository.GetByIdForUpdateAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns(branch);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        branch.SelfServiceEmployeeId.Should().BeNull();

        // EmployeeId nulo -> o handler não deve nem consultar o repositório de funcionários.
        await _employeeRepository.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        // Commit explícito do handler no fim do fluxo + commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidEmployeeSameBranch_ShouldSetSelfServiceEmployeeAndCommitTwice()
    {
        var branch = CreateActiveBranch();
        var employee = CreateActiveEmployee(branchId: 1);
        var command = new SetSelfServiceEmployeeCommand(BranchId: 1, EmployeeId: 5);
        _branchRepository.GetByIdForUpdateAsync(command.BranchId, Arg.Any<CancellationToken>())
            .Returns(branch);
        _employeeRepository.GetByIdAsync(command.EmployeeId.GetValueOrDefault(), Arg.Any<CancellationToken>())
            .Returns(employee);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        branch.SelfServiceEmployeeId.Should().Be(command.EmployeeId);

        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }
}
