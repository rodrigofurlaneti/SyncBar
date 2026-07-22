using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Users.GetRoles;

internal sealed class GetRolesQueryHandler(IRoleRepository roleRepository)
    : IQueryHandler<GetRolesQuery, IReadOnlyCollection<RoleResponse>>
{
    public async Task<Result<IReadOnlyCollection<RoleResponse>>> Handle(
        GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await roleRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);

        IReadOnlyCollection<RoleResponse> response = roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleResponse(r.Id, r.Name, r.Description))
            .ToList();

        return Result.Success(response);
    }
}
