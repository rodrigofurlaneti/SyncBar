using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Users.GetRoles;

public sealed record GetRolesQuery(long CompanyId) : IQuery<IReadOnlyCollection<RoleResponse>>;
