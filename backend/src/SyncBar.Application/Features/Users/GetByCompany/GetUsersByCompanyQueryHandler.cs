using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Users.GetByCompany;

internal sealed class GetUsersByCompanyQueryHandler(
    IAppUserRepository userRepository,
    IUserRoleRepository userRoleRepository)
    : IQueryHandler<GetUsersByCompanyQuery, IReadOnlyCollection<UserResponse>>
{
    public async Task<Result<IReadOnlyCollection<UserResponse>>> Handle(
        GetUsersByCompanyQuery request, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);
        var userIds = users.Select(u => u.Id).ToList();
        var links = await userRoleRepository.GetByUsersAsync(userIds, cancellationToken);

        IReadOnlyCollection<UserResponse> response = users
            .OrderBy(u => u.UserName)
            .Select(u => new UserResponse(
                u.Id, u.UserName, u.Email, u.EmployeeId, u.IsActive,
                links.Where(l => l.AppUserId == u.Id).Select(l => l.RoleId).ToList()))
            .ToList();

        return Result.Success(response);
    }
}
