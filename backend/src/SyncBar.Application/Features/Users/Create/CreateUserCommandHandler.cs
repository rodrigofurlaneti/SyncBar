using SyncBar.Application.Abstractions.Authentication;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Users.Create;

internal sealed class CreateUserCommandHandler(
    IAppUserRepository userRepository,
    IRoleRepository roleRepository,
    IUserRoleRepository userRoleRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateUserCommand, long>
{
    public async Task<Result<long>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsAsync(request.UserName, request.Email, cancellationToken))
            return Result.Failure<long>(new Error("AppUser.AlreadyExists", "User name or e-mail already in use."));

        // Perfis precisam existir e pertencer a mesma empresa.
        foreach (var roleId in request.RoleIds.Distinct())
        {
            var role = await roleRepository.GetByIdAsync(roleId, cancellationToken);
            if (role is null || !role.IsActive || role.CompanyId != request.CompanyId)
                return Result.Failure<long>(new Error("Role.NotFound", $"Role {roleId} not found for this company."));
        }

        // Senha NUNCA em texto puro — hash BCrypt (workFactor 12).
        var passwordHash = passwordHasher.Hash(request.Password);

        var user = AppUser.Create(request.CompanyId, request.EmployeeId, request.UserName, request.Email, passwordHash);
        if (user.IsFailure)
            return Result.Failure<long>(user.Error);

        await userRepository.AddAsync(user.Value, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        foreach (var roleId in request.RoleIds.Distinct())
        {
            var link = UserRole.Create(user.Value.Id, roleId);
            if (link.IsSuccess)
                await userRoleRepository.AddAsync(link.Value, cancellationToken);
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success(user.Value.Id);
    }
}
