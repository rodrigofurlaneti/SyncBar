using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class DiningTableRepository(AppDbContext context) : IDiningTableRepository
{
    public async Task<DiningTable?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.DiningTables.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<DiningTable?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.DiningTables.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<DiningTable>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.DiningTables.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(DiningTable entity, CancellationToken cancellationToken = default)
        => await context.DiningTables.AddAsync(entity, cancellationToken);
}
