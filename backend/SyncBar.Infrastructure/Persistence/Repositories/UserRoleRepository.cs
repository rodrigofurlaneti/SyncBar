using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class UserRoleRepository(AppDbContext context) : IUserRoleRepository
{
    // Tracked — atribuicao de perfis desativa/reativa vinculos.
    public async Task<IReadOnlyCollection<UserRole>> GetByUserForUpdateAsync(long appUserId, CancellationToken cancellationToken = default)
        => await context.UserRoles
            .Where(x => x.AppUserId == appUserId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<UserRole>> GetByUsersAsync(IReadOnlyCollection<long> appUserIds, CancellationToken cancellationToken = default)
        => await context.UserRoles.AsNoTracking()
            .Where(x => appUserIds.Contains(x.AppUserId) && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(UserRole entity, CancellationToken cancellationToken = default)
        => await context.UserRoles.AddAsync(entity, cancellationToken);
}
