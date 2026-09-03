using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Features.Employees.RegisterTeamMember;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using Xunit;

namespace SyncBar.Tests.Application.Features.Employees.RegisterTeamMember;

public sealed class RegisterTeamMemberCommandHandlerTests
{
    private readonly IEmployeeRepository _employeeRepository = Substitute.For<IEmployeeRepository>();
    private readonly IJobTitleRepository _jobTitleRepository = Substitute.For<IJobTitleRepository>();
    private readonly IAppUserRepository _userRepository = Substitute.For<IAppUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IUserRoleRepository _userRoleRepository = Substitute.For<IUserRoleRepository>();
    private readonly IAppFeatureRepository _featureRepository = Substitute.For<IAppFeatureRepository>();
    private readonly IAppUserFeatureRepository _userFeatureRepository = Substitute.For<IAppUserFeatureRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RegisterTeamMemberCommandHandler _handler;

    public RegisterTeamMemberCommandHandlerTests()
    {
        _handler = new RegisterTeamMemberCommandHandler(
            _employeeRepository, _jobTitleRepository, _userRepository, _roleRepository, _userRoleRepository,
            _featureRepository, _userFeatureRepository, _passwordHasher, _logRepository, _unitOfWork);
    }

    private static RegisterTeamMemberCommand CreateValidCommand(
        long branchId = 1,
        long companyId = 1,
        long jobTitleId = 1,
        string name = "Funcionario Teste",
        string cpf = "12345678900",
        string? email = null,
        string? phone = null,
        decimal? salary = null,
        bool hasSystemAccess = false,
        string? userName = null,
        string? userEmail = null,
        string? password = null,
        IReadOnlyCollection<long>? extraFeatureIds = null)
        => new(
            branchId, companyId, jobTitleId, name, cpf, email, phone, DateTime.Now, salary,
            hasSystemAccess,
            userName ?? (hasSystemAccess ? "usuario.teste" : null),
            userEmail ?? (hasSystemAccess ? "usuario@teste.com" : null),
            password ?? (hasSystemAccess ? "SenhaForte123" : null),
            extraFeatureIds);

    private static JobTitle CreateActiveJobTitle(long companyId = 1, string name = "Garcom")
        => JobTitle.Create(companyId, name).Value;

    private static Role CreateActiveRole(long companyId = 1, string name = "Garcom")
        => Role.Create(companyId, name, null).Value;

    private void SetupCpfAndJobTitleHappyPath(RegisterTeamMemberCommand command, JobTitle jobTitle)
    {
        _employeeRepository.ExistsByCpfAsync(command.Cpf, Arg.Any<CancellationToken>()).Returns(false);
        _jobTitleRepository.GetByIdAsync(command.JobTitleId, Arg.Any<CancellationToken>()).Returns(jobTitle);
    }

    [Fact]
    public async Task Handle_CpfAlreadyInUseByActiveEmployee_ShouldReturnFailureWithoutPersisting()
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
        SetupCpfAndJobTitleHappyPath(command, jobTitle);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("JobTitle.NotFound");
        await _employeeRepository.DidNotReceive().AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmployeeNameEmpty_ShouldReturnFailureWithoutPersisting()
    {
        var command = CreateValidCommand(name: "");
        var jobTitle = CreateActiveJobTitle();
        SetupCpfAndJobTitleHappyPath(command, jobTitle);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.EmptyName");
        await _employeeRepository.DidNotReceive().AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HasSystemAccessFalse_ShouldPersistOnlyEmployeeAndReturnSuccessWithoutAppUser()
    {
        var command = CreateValidCommand(hasSystemAccess: false);
        var jobTitle = CreateActiveJobTitle();
        SetupCpfAndJobTitleHappyPath(command, jobTitle);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AppUserId.Should().BeNull();
        result.Value.AccessWarning.Should().BeNull();

        await _employeeRepository.Received(1).AddAsync(
            Arg.Is<Employee>(e => e.Name == command.Name && e.Cpf == command.Cpf),
            Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().ExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>());
        // 1 commit explícito (Employee) + 1 commit do finally da base.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HasSystemAccessTrueUserNameOrEmailAlreadyInUse_ShouldReturnSuccessWithWarningAndNoAppUser()
    {
        var command = CreateValidCommand(hasSystemAccess: true);
        var jobTitle = CreateActiveJobTitle();
        SetupCpfAndJobTitleHappyPath(command, jobTitle);
        _userRepository.ExistsAsync(command.UserName!, command.UserEmail!, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        // Degradação graciosa: sucesso parcial, não falha — o funcionário já foi persistido.
        result.IsSuccess.Should().BeTrue();
        result.Value.AppUserId.Should().BeNull();
        result.Value.AccessWarning.Should().NotBeNullOrEmpty();
        result.Value.AccessWarning.Should().Contain("já está em uso");

        await _employeeRepository.Received(1).AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>());
        // 1 commit explícito (Employee) + 1 commit do finally — não chega ao commit do usuário.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HasSystemAccessTrueUserNameBlank_ShouldReturnSuccessWithWarningAndNoAppUserPersisted()
    {
        // Bypassa o FluentValidation (que roda só no pipeline do MediatR) para exercitar a
        // validação de negócio real feita por AppUser.Create quando UserName está em branco.
        var command = CreateValidCommand(hasSystemAccess: true, userName: " ");
        var jobTitle = CreateActiveJobTitle();
        SetupCpfAndJobTitleHappyPath(command, jobTitle);
        _userRepository.ExistsAsync(command.UserName!, command.UserEmail!, Arg.Any<CancellationToken>()).Returns(false);
        _roleRepository.GetByNameAsync(command.CompanyId, jobTitle.Name, Arg.Any<CancellationToken>())
            .Returns(CreateActiveRole(command.CompanyId, jobTitle.Name));
        _passwordHasher.Hash(command.Password!).Returns("hash-fake");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AppUserId.Should().BeNull();
        result.Value.AccessWarning.Should().Be("UserName is required.");

        await _userRepository.DidNotReceive().AddAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>());
        await _roleRepository.DidNotReceive().AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
        // 1 commit explícito (Employee) + 1 do finally — falha antes do commit do usuário.
        await _unitOfWork.Received(2).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HasSystemAccessTrueRoleDoesNotExistForJobTitle_ShouldAutoProvisionRoleAndCreateUserAndLink()
    {
        var command = CreateValidCommand(hasSystemAccess: true);
        var jobTitle = CreateActiveJobTitle(name: "Gerente");
        SetupCpfAndJobTitleHappyPath(command, jobTitle);
        _userRepository.ExistsAsync(command.UserName!, command.UserEmail!, Arg.Any<CancellationToken>()).Returns(false);
        _roleRepository.GetByNameAsync(command.CompanyId, jobTitle.Name, Arg.Any<CancellationToken>()).Returns((Role?)null);
        _passwordHasher.Hash(command.Password!).Returns("hash-fake");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AppUserId.Should().Be(0); // Id sempre 0 em teste — fábrica não expõe setter.
        result.Value.AccessWarning.Should().BeNull();

        await _roleRepository.Received(1).AddAsync(
            Arg.Is<Role>(r => r.Name == "Gerente" && r.CompanyId == command.CompanyId),
            Arg.Any<CancellationToken>());
        await _userRepository.Received(1).AddAsync(
            Arg.Is<AppUser>(u =>
                u.CompanyId == command.CompanyId &&
                u.UserName == command.UserName &&
                u.Email == command.UserEmail &&
                u.PasswordHash == "hash-fake"),
            Arg.Any<CancellationToken>());
        await _userRoleRepository.Received(1).AddAsync(Arg.Any<UserRole>(), Arg.Any<CancellationToken>());
        // Sem ExtraFeatureIds, o repositório de features nem é consultado.
        await _featureRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
        // Employee(1) + AppUser/Role(1) + commit final do handler(1) + finally da base(1) = 4.
        await _unitOfWork.Received(4).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HasSystemAccessTrueRoleAlreadyExistsForJobTitle_ShouldReuseRoleWithoutCreatingNewOne()
    {
        var command = CreateValidCommand(hasSystemAccess: true);
        var jobTitle = CreateActiveJobTitle(name: "Gerente");
        var existingRole = CreateActiveRole(command.CompanyId, "Gerente");
        SetupCpfAndJobTitleHappyPath(command, jobTitle);
        _userRepository.ExistsAsync(command.UserName!, command.UserEmail!, Arg.Any<CancellationToken>()).Returns(false);
        _roleRepository.GetByNameAsync(command.CompanyId, jobTitle.Name, Arg.Any<CancellationToken>()).Returns(existingRole);
        _passwordHasher.Hash(command.Password!).Returns("hash-fake");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _roleRepository.DidNotReceive().AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
        await _userRepository.Received(1).AddAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>());
        await _userRoleRepository.Received(1).AddAsync(
            Arg.Is<UserRole>(l => l.CompanyId == command.CompanyId && l.RoleId == existingRole.Id),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(4).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExtraFeatureIdNotAValidFeature_ShouldReturnSuccessWithWarningAndSkipInvalidFeature()
    {
        var command = CreateValidCommand(hasSystemAccess: true, extraFeatureIds: [999]);
        var jobTitle = CreateActiveJobTitle(name: "Gerente");
        var existingRole = CreateActiveRole(command.CompanyId, "Gerente");
        SetupCpfAndJobTitleHappyPath(command, jobTitle);
        _userRepository.ExistsAsync(command.UserName!, command.UserEmail!, Arg.Any<CancellationToken>()).Returns(false);
        _roleRepository.GetByNameAsync(command.CompanyId, jobTitle.Name, Arg.Any<CancellationToken>()).Returns(existingRole);
        _passwordHasher.Hash(command.Password!).Returns("hash-fake");
        _featureRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        var result = await _handler.Handle(command, CancellationToken.None);

        // O AppUser foi criado com sucesso — só a feature extra é que foi ignorada.
        result.IsSuccess.Should().BeTrue();
        result.Value.AppUserId.Should().Be(0);
        result.Value.AccessWarning.Should().Contain("999").And.Contain("não é uma tela válida");
        await _userFeatureRepository.DidNotReceive().AddAsync(Arg.Any<AppUserFeature>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(4).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExtraFeatureIdsNullOrEmpty_ShouldNotCallFeatureRepositoryAtAll()
    {
        var command = CreateValidCommand(hasSystemAccess: true, extraFeatureIds: null);
        var jobTitle = CreateActiveJobTitle(name: "Gerente");
        var existingRole = CreateActiveRole(command.CompanyId, "Gerente");
        SetupCpfAndJobTitleHappyPath(command, jobTitle);
        _userRepository.ExistsAsync(command.UserName!, command.UserEmail!, Arg.Any<CancellationToken>()).Returns(false);
        _roleRepository.GetByNameAsync(command.CompanyId, jobTitle.Name, Arg.Any<CancellationToken>()).Returns(existingRole);
        _passwordHasher.Hash(command.Password!).Returns("hash-fake");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessWarning.Should().BeNull();
        await _featureRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExtraFeatureIdMatchesExistingFeature_ShouldNotPersistLinkBecauseCreatedAppUserHasZeroId()
    {
        // Observação de comportamento real (não limitação do teste): AppUserFeature.Create exige
        // AppUserId > 0, mas o AppUser criado via factory pública nunca tem Id atribuído fora do
        // EF Core — em teste ele fica sempre 0. Logo, mesmo com um Id de feature que bate com um
        // AppFeature existente, o vínculo AppUserFeature nunca é persistido nesse cenário: a
        // criação falha silenciosamente (link.IsSuccess == false) e o loop simplesmente segue sem
        // reportar erro nem aviso. Em produção o EF atribui um Id > 0 ao AppUser antes deste ponto,
        // então esse caminho normalmente teria sucesso — aqui documentamos o comportamento
        // observado com os dublês de teste, não um bug de produção.
        var existingFeature = AppFeature.Create("CAIXA", "Caixa").Value; // Id == 0 em teste.
        var command = CreateValidCommand(hasSystemAccess: true, extraFeatureIds: [existingFeature.Id]);
        var jobTitle = CreateActiveJobTitle(name: "Gerente");
        var existingRole = CreateActiveRole(command.CompanyId, "Gerente");
        SetupCpfAndJobTitleHappyPath(command, jobTitle);
        _userRepository.ExistsAsync(command.UserName!, command.UserEmail!, Arg.Any<CancellationToken>()).Returns(false);
        _roleRepository.GetByNameAsync(command.CompanyId, jobTitle.Name, Arg.Any<CancellationToken>()).Returns(existingRole);
        _passwordHasher.Hash(command.Password!).Returns("hash-fake");
        _featureRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([existingFeature]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessWarning.Should().BeNull();
        await _userFeatureRepository.DidNotReceive().AddAsync(Arg.Any<AppUserFeature>(), Arg.Any<CancellationToken>());
    }
}
