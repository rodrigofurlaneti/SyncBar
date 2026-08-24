using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Users.UpdateRoles;

internal sealed class UpdateUserRolesCommandHandler : BaseCommandHandler<UpdateUserRolesCommand>
{
    private readonly IAppUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUnitOfWork _unitOfWork;

    // ✅ Construtor Tradicional
    public UpdateUserRolesCommandHandler(
        IAppUserRepository userRepository,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(UpdateUserRolesCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(UpdateUserRolesCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do administrador que está alterando as roles, preencha:
                // userIdBox.Value = request.AdminUserId;

                var user = await _userRepository.GetByIdAsync(request.AppUserId, cancellationToken);
                if (user is null || !user.IsActive)
                    return Result.Failure(new Error("AppUser.NotFound", "User not found."));

                var desired = request.RoleIds.Distinct().ToHashSet();

                var validationFailure = await ValidateRolesAsync(desired, user.CompanyId, cancellationToken);
                if (validationFailure is not null)
                    return validationFailure;

                // Soft delete: vinculos removidos são desativados, nunca apagados.
                var currentLinks = await _userRoleRepository.GetByUserForUpdateAsync(user.Id, cancellationToken);

                DeactivateRemovedRoles(currentLinks, desired);
                await AddMissingRolesAsync(currentLinks, desired, user.CompanyId, user.Id, cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }

    private async Task<Result?> ValidateRolesAsync(HashSet<long> desiredRoleIds, long companyId, CancellationToken cancellationToken)
    {
        foreach (var roleId in desiredRoleIds)
        {
            var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
            if (role is null || !role.IsActive || role.CompanyId != companyId)
                return Result.Failure(new Error("Role.NotFound", $"Role {roleId} not found for this company."));
        }

        return null;
    }

    private static void DeactivateRemovedRoles(IEnumerable<UserRole> currentLinks, HashSet<long> desiredRoleIds)
    {
        foreach (var link in currentLinks.Where(l => l.IsActive && !desiredRoleIds.Contains(l.RoleId)))
            link.Deactivate();
    }

    private async Task AddMissingRolesAsync(
        IEnumerable<UserRole> currentLinks,
        HashSet<long> desiredRoleIds,
        long companyId,
        long userId,
        CancellationToken cancellationToken)
    {
        var activeRoleIds = currentLinks.Where(l => l.IsActive).Select(l => l.RoleId).ToHashSet();

        foreach (var roleId in desiredRoleIds.Except(activeRoleIds))
        {
            var link = UserRole.Create(companyId, userId, roleId);
            if (link.IsSuccess)
                await _userRoleRepository.AddAsync(link.Value, cancellationToken);
        }
    }
}