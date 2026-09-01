using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Users.Create;

internal sealed class CreateUserCommandHandler : BaseCommandHandler<CreateUserCommand, long>
{
    private readonly IAppUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(
        IAppUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IPasswordHasher passwordHasher,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<long>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CreateUserCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do administrador que está criando o usuário, preencha:

                // AppUser só pode ser vinculado a um Employee existente e ativo (evita FK órfã/exceção de banco).
                if (request.EmployeeId is not null)
                {
                    var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId.Value, cancellationToken);
                    if (employee is null || !employee.IsActive)
                        return Result.Failure<long>(new Error("Employee.NotFound", "Employee not found."));
                }

                if (await _userRepository.ExistsAsync(request.UserName, request.Email, cancellationToken))
                    return Result.Failure<long>(new Error("AppUser.AlreadyExists", "User name or e-mail already in use."));

                // Perfis precisam existir e pertencer a mesma empresa.
                // RoleIds materializado uma única vez — evita múltipla enumeração do IEnumerable de entrada.
                var roleIds = request.RoleIds.Distinct().ToList();
                foreach (var roleId in roleIds)
                {
                    var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
                    if (role is null || !role.IsActive || role.CompanyId != request.CompanyId)
                        return Result.Failure<long>(new Error("Role.NotFound", $"Role {roleId} not found for this company."));
                }

                // Senha NUNCA em texto puro — hash BCrypt (workFactor 12).
                var passwordHash = _passwordHasher.Hash(request.Password);

                var user = AppUser.Create(request.CompanyId, request.EmployeeId, request.UserName, request.Email, passwordHash);
                if (user.IsFailure)
                    return Result.Failure<long>(user.Error);

                await _userRepository.AddAsync(user.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                foreach (var roleId in roleIds)
                {
                    var link = UserRole.Create(user.Value.CompanyId, user.Value.Id, roleId);
                    if (link.IsSuccess)
                        await _userRoleRepository.AddAsync(link.Value, cancellationToken);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(user.Value.Id);
            });
    }
}