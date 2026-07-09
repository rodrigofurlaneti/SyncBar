using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class RoleRepository(AppDbContext context) : IRoleRepository
{
    public async Task<Role?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Role>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default)
        => await context.Roles.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Role entity, CancellationToken cancellationToken = default)
        => await context.Roles.AddAsync(entity, cancellationToken);
}
