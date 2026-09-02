using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Employees.RegisterTeamMember;

internal sealed class RegisterTeamMemberCommandHandler : BaseCommandHandler<RegisterTeamMemberCommand, RegisterTeamMemberResult>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IJobTitleRepository _jobTitleRepository;
    private readonly IAppUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IAppFeatureRepository _featureRepository;
    private readonly IAppUserFeatureRepository _userFeatureRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterTeamMemberCommandHandler(
        IEmployeeRepository employeeRepository,
        IJobTitleRepository jobTitleRepository,
        IAppUserRepository userRepository,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IAppFeatureRepository featureRepository,
        IAppUserFeatureRepository userFeatureRepository,
        IPasswordHasher passwordHasher,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _jobTitleRepository = jobTitleRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _featureRepository = featureRepository;
        _userFeatureRepository = userFeatureRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<RegisterTeamMemberResult>> Handle(RegisterTeamMemberCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(RegisterTeamMemberCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Mesma ordem/regras de CreateEmployeeCommandHandler: CPF único entre ativos
                // (espelha UQ_Employee_Cpf filtrado) e Cargo precisa existir e estar ativo.
                if (await _employeeRepository.ExistsByCpfAsync(request.Cpf, cancellationToken))
                    return Result.Failure<RegisterTeamMemberResult>(new Error("Employee.CpfAlreadyExists", "An active employee with this CPF already exists."));

                var jobTitle = await _jobTitleRepository.GetByIdAsync(request.JobTitleId, cancellationToken);
                if (jobTitle is null || !jobTitle.IsActive)
                    return Result.Failure<RegisterTeamMemberResult>(new Error("JobTitle.NotFound", "Job title not found."));

                var employee = Employee.Create(
                    request.BranchId, request.JobTitleId, request.Name, request.Cpf,
                    request.Email, request.Phone, request.HiredAt, null, request.Salary);
                if (employee.IsFailure)
                    return Result.Failure<RegisterTeamMemberResult>(employee.Error);

                await _employeeRepository.AddAsync(employee.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                // Cargo sem "usa o sistema" marcado (ex.: auxiliar de limpeza, vigilante) — só o
                // funcionário é cadastrado, sem AppUser. É o caso mais comum do card.
                if (!request.HasSystemAccess)
                    return Result.Success(new RegisterTeamMemberResult(employee.Value.Id, null, null));

                return await CreateSystemUserAsync(request, employee.Value, jobTitle.Name, cancellationToken);
            });
    }

    private async Task<Result<RegisterTeamMemberResult>> CreateSystemUserAsync(
        RegisterTeamMemberCommand request, Employee employee, string jobTitleName, CancellationToken cancellationToken)
    {
        // Degradação graciosa: se o usuário/e-mail já existir, o funcionário cadastrado acima
        // continua válido — reportamos como sucesso parcial (com aviso) em vez de falha, para não
        // fazer a pessoa perder os dados já digitados do funcionário.
        if (await _userRepository.ExistsAsync(request.UserName!, request.UserEmail!, cancellationToken))
            return Result.Success(new RegisterTeamMemberResult(
                employee.Id, null,
                "Funcionário cadastrado, mas o nome de usuário ou e-mail informado já está em uso — crie o acesso ao sistema na tela Usuários."));

        // Perfil auto-provisionado a partir do Cargo — mesma regra do CreateUserCommandHandler
        // (ver comentário lá). Mantém [Authorize(Roles=...)] funcionando sem pedir um "Perfil"
        // separado nesta tela.
        var role = await _roleRepository.GetByNameAsync(request.CompanyId, jobTitleName, cancellationToken);
        if (role is null)
        {
            var newRole = Role.Create(
                request.CompanyId,
                jobTitleName,
                $"Perfil gerado automaticamente a partir do cargo \"{jobTitleName}\".");
            if (newRole.IsFailure)
                return Result.Success(new RegisterTeamMemberResult(employee.Id, null, newRole.Error.Message));

            role = newRole.Value;
            await _roleRepository.AddAsync(role, cancellationToken);
        }

        var passwordHash = _passwordHasher.Hash(request.Password!);
        var user = AppUser.Create(request.CompanyId, employee.Id, request.UserName!, request.UserEmail!, passwordHash);
        if (user.IsFailure)
            return Result.Success(new RegisterTeamMemberResult(employee.Id, null, user.Error.Message));

        await _userRepository.AddAsync(user.Value, cancellationToken);
        // Único commit para o usuário e (se novo) o perfil auto-provisionado — garante os Ids
        // gerados por identidade antes de criar o vínculo UserRole abaixo.
        await _unitOfWork.CommitAsync(cancellationToken);

        var link = UserRole.Create(user.Value.CompanyId, user.Value.Id, role.Id);
        if (link.IsSuccess)
            await _userRoleRepository.AddAsync(link.Value, cancellationToken);

        var warning = await GrantExtraFeaturesAsync(request.ExtraFeatureIds, user.Value.Id, cancellationToken);

        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(new RegisterTeamMemberResult(employee.Id, user.Value.Id, warning));
    }

    // Exceções de acesso por pessoa ALÉM do que o Cargo já concede por padrão via JobTitleFeature
    // (ex.: "esse garçom específico também vê o Caixa"). Não duplica os acessos do cargo — só as
    // telas extras marcadas nesta tela de cadastro, equivalente ao modo "Por pessoa" da tela
    // Acessos, feito aqui sem precisar navegar para outra tela.
    private async Task<string?> GrantExtraFeaturesAsync(
        IReadOnlyCollection<long>? extraFeatureIds, long appUserId, CancellationToken cancellationToken)
    {
        if (extraFeatureIds is null || extraFeatureIds.Count == 0)
            return null;

        var validFeatureIds = (await _featureRepository.GetAllAsync(cancellationToken))
            .Select(f => f.Id)
            .ToHashSet();

        foreach (var featureId in extraFeatureIds.Distinct())
        {
            if (!validFeatureIds.Contains(featureId))
                return $"Usuário criado, mas o acesso extra \"{featureId}\" não é uma tela válida e foi ignorado.";

            var link = AppUserFeature.Create(appUserId, featureId);
            if (link.IsSuccess)
                await _userFeatureRepository.AddAsync(link.Value, cancellationToken);
        }

        return null;
    }
}
