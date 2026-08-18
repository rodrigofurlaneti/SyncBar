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
                foreach (var roleId in desired)
                {
                    var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
                    if (role is null || !role.IsActive || role.CompanyId != user.CompanyId)
                        return Result.Failure(new Error("Role.NotFound", $"Role {roleId} not found for this company."));
                }

                // Soft delete: vinculos removidos são desativados, nunca apagados.
                var currentLinks = await _userRoleRepository.GetByUserForUpdateAsync(user.Id, cancellationToken);

                foreach (var link in currentLinks.Where(l => l.IsActive && !desired.Contains(l.RoleId)))
                    link.Deactivate();

                var activeRoleIds = currentLinks.Where(l => l.IsActive).Select(l => l.RoleId).ToHashSet();
                foreach (var roleId in desired.Except(activeRoleIds))
                {
                    var link = UserRole.Create(user.CompanyId, user.Id, roleId);
                    if (link.IsSuccess)
                        await _userRoleRepository.AddAsync(link.Value, cancellationToken);
                }

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}