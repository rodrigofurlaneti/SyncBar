using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Features.Users.Create;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Users.Create;

public sealed class CreateUserCommandHandlerTests
{
    private readonly IAppUserRepository _userRepository = Substitute.For<IAppUserRepository>();
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IUserRoleRepository _userRoleRepository = Substitute.For<IUserRoleRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _handler = new CreateUserCommandHandler(
            _userRepository, _employeeRepository, _roleRepository, _userRoleRepository,
            _passwordHasher, _logRepository, _unitOfWork);
    }

    private static Role CreateActiveRole(long companyId = 1, string name = "Gerente")
        => Role.Create(companyId, name, null).Value;

    private static Employee CreateActiveEmployee()
        => Employee.Create(
            branchId: 1, jobTitleId: 1, name: "Funcionario Teste", cpf: "12345678900",
            email: null, phone: null, hiredAt: DateTime.Now, dismissedAt: null, salary: null).Value;

    private static CreateUserCommand CreateValidCommand(long? employeeId = null, IReadOnlyCollection<long>? roleIds = null)
        => new(
            CompanyId: 1,
            EmployeeId: employeeId,
            UserName: "usuario.teste",
            Email: "usuario@teste.com",
            Password: "SenhaForte123",
            RoleIds: roleIds ?? [1]);

    [Fact]
    public async Task Handle_EmployeeIdProvidedButNotFound_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand(employeeId: 99);
        _employeeRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.NotFound");
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>());
        // Sem persistência: só resta o commit do finally da base.
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmployeeInactive_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand(employeeId: 5);
        var employee = CreateActiveEmployee();
        employee.Deactivate();
        _employeeRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.NotFound");
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNameOrEmailAlreadyInUse_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand();
        _userRepository.ExistsAsync(command.UserName, command.Email, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AppUser.AlreadyExists");
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RoleNotFoundForCompany_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand(roleIds: [1, 2]);
        _userRepository.ExistsAsync(command.UserName, command.Email, Arg.Any<CancellationToken>()).Returns(false);
        _roleRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateActiveRole());
        _roleRepository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns((Role?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Role.NotFound");
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RoleFromDifferentCompany_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand(roleIds: [1]);
        _userRepository.ExistsAsync(command.UserName, command.Email, Arg.Any<CancellationToken>()).Returns(false);
        _roleRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateActiveRole(companyId: 2));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Role.NotFound");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequestWithoutEmployee_ShouldHashPasswordAndPersistUserAndRoles()
    {
        var command = CreateValidCommand(roleIds: [1, 2]);
        _userRepository.ExistsAsync(command.UserName, command.Email, Arg.Any<CancellationToken>()).Returns(false);
        _roleRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateActiveRole());
        _roleRepository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(CreateActiveRole());
        _passwordHasher.Hash(command.Password).Returns("hash-fake");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Id é sempre 0 em teste: a fábrica pública não expõe forma de setá-lo.
        result.Value.Should().Be(0);

        await _userRepository.Received(1).AddAsync(
            Arg.Is<AppUser>(u =>
                u.CompanyId == command.CompanyId &&
                u.EmployeeId == null &&
                u.UserName == command.UserName &&
                u.Email == command.Email &&
                u.PasswordHash == "hash-fake" &&
                u.IsActive),
            Arg.Any<CancellationToken>());

        await _userRoleRepository.Received(2).AddAsync(Arg.Any<UserRole>(), Arg.Any<CancellationToken>());
        // 2 commits explícitos do handler (usuário + roles) + 1 commit do finally da base.
        await _unitOfWork.Received(3).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequestWithActiveEmployee_ShouldLinkEmployeeToUser()
    {
        var command = CreateValidCommand(employeeId: 7, roleIds: [1]);
        var employee = CreateActiveEmployee();
        _employeeRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(employee);
        _userRepository.ExistsAsync(command.UserName, command.Email, Arg.Any<CancellationToken>()).Returns(false);
        _roleRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateActiveRole());
        _passwordHasher.Hash(command.Password).Returns("hash-fake");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _userRepository.Received(1).AddAsync(
            Arg.Is<AppUser>(u => u.EmployeeId == 7),
            Arg.Any<CancellationToken>());
        // 2 commits explícitos do handler (usuário + roles, mesmo com 1 role só) + 1 do finally.
        await _unitOfWork.Received(3).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateRoleIds_ShouldValidateAndPersistEachDistinctRoleOnlyOnce()
    {
        var command = CreateValidCommand(roleIds: [1, 1, 2]);
        _userRepository.ExistsAsync(command.UserName, command.Email, Arg.Any<CancellationToken>()).Returns(false);
        _roleRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(CreateActiveRole());
        _roleRepository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(CreateActiveRole());
        _passwordHasher.Hash(command.Password).Returns("hash-fake");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // RoleIds.Distinct() reduz [1,1,2] a [1,2] — só 2 validações e 2 vínculos, não 3.
        await _roleRepository.Received(1).GetByIdAsync(1, Arg.Any<CancellationToken>());
        await _userRoleRepository.Received(2).AddAsync(Arg.Any<UserRole>(), Arg.Any<CancellationToken>());
    }
}
