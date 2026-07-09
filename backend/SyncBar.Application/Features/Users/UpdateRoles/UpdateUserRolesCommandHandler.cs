using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Users.UpdateRoles;

internal sealed class UpdateUserRolesCommandHandler(
    IAppUserRepository userRepository,
    IRoleRepository roleRepository,
    IUserRoleRepository userRoleRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateUserRolesCommand>
{
    public async Task<Result> Handle(UpdateUserRolesCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.AppUserId, cancellationToken);
        if (user is null || !user.IsActive)
            return Result.Failure(new Error("AppUser.NotFound", "User not found."));

        var desired = request.RoleIds.Distinct().ToHashSet();
        foreach (var roleId in desired)
        {
            var role = await roleRepository.GetByIdAsync(roleId, cancellationToken);
            if (role is null || !role.IsActive || role.CompanyId != user.CompanyId)
                return Result.Failure(new Error("Role.NotFound", $"Role {roleId} not found for this company."));
        }

        // Soft delete: vinculos removidos sao desativados, nunca apagados.
        var currentLinks = await userRoleRepository.GetByUserForUpdateAsync(user.Id, cancellationToken);

        foreach (var link in currentLinks.Where(l => l.IsActive && !desired.Contains(l.RoleId)))
            link.Deactivate();

        var activeRoleIds = currentLinks.Where(l => l.IsActive).Select(l => l.RoleId).ToHashSet();
        foreach (var roleId in desired.Except(activeRoleIds))
        {
            var link = UserRole.Create(user.Id, roleId);
            if (link.IsSuccess)
                await userRoleRepository.AddAsync(link.Value, cancellationToken);
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
