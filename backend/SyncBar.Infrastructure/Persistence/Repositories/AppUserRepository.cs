using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class AppUserRepository(AppDbContext context) : IAppUserRepository
{
    public async Task<AppUser?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.AppUsers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<AppUser?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.AppUsers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<AppUser>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
        => await context.AppUsers.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(cancellationToken);

    // Tracked — Login atualiza FailedAccessCount/LastLoginAt.
    public async Task<AppUser?> GetByUserNameForUpdateAsync(string userName, CancellationToken cancellationToken = default)
        => await context.AppUsers
            .FirstOrDefaultAsync(x => x.UserName == userName && x.IsActive, cancellationToken);

    public async Task<bool> ExistsAsync(string userName, string email, CancellationToken cancellationToken = default)
        => await context.AppUsers.AsNoTracking()
            .AnyAsync(x => (x.UserName == userName || x.Email == email) && x.IsActive, cancellationToken);

    public async Task<IReadOnlyCollection<string>> GetRoleNamesAsync(long appUserId, CancellationToken cancellationToken = default)
        => await context.UserRoles.AsNoTracking()
            .Where(ur => ur.AppUserId == appUserId && ur.IsActive)
            .Join(context.Roles.Where(r => r.IsActive), ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<string>> GetPermissionCodesAsync(long appUserId, CancellationToken cancellationToken = default)
        => await context.UserRoles.AsNoTracking()
            .Where(ur => ur.AppUserId == appUserId && ur.IsActive)
            .Join(context.RolePermissions.Where(rp => rp.IsActive), ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp.PermissionId)
            .Join(context.Permissions.Where(p => p.IsActive), pid => pid, p => p.Id, (pid, p) => p.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AppUser entity, CancellationToken cancellationToken = default)
        => await context.AppUsers.AddAsync(entity, cancellationToken);
}
