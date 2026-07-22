using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Users.UpdateRoles;

public sealed record UpdateUserRolesCommand(
    long AppUserId,
    IReadOnlyCollection<long> RoleIds) : ICommand;
