using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Users.GetRoles;

internal sealed class GetRolesQueryHandler(
    IRoleRepository roleRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetRolesQuery, IReadOnlyCollection<RoleResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<RoleResponse>>> Handle(
        GetRolesQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetRolesQueryHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do administrador consultando os perfis, preencha:

                var roles = await roleRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);

                IReadOnlyCollection<RoleResponse> response = roles
                    .OrderBy(r => r.Name)
                    .Select(r => new RoleResponse(r.Id, r.Name, r.Description))
                    .ToList();

                return Result.Success(response);
            });
    }
}