using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class DiningAreaTableRepository(AppDbContext context) : IDiningAreaTableRepository
{
    public async Task<DiningAreaTable?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.Set<DiningAreaTable>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<IEnumerable<DiningAreaTable>> GetByDiningAreaIdAsync(long diningAreaId, CancellationToken cancellationToken = default)
        => await context.Set<DiningAreaTable>()
            .AsNoTracking()
            .Where(x => x.DiningAreaId == diningAreaId && x.IsActive)
            .ToListAsync(cancellationToken);
    public async Task<bool> ExistsByTableIdAsync(long diningTableId, CancellationToken cancellationToken = default)
        => await context.Set<DiningAreaTable>()
            .AsNoTracking()
            .AnyAsync(x => x.DiningTableId == diningTableId && x.IsActive, cancellationToken);
    public async Task<DiningAreaTable?> GetByTableIdAsync(long diningTableId, CancellationToken cancellationToken = default)
    {
        return await context.Set<DiningAreaTable>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DiningTableId == diningTableId && x.IsActive, cancellationToken);
    }
    public async Task AddAsync(DiningAreaTable entity, CancellationToken cancellationToken = default)
        => await context.Set<DiningAreaTable>().AddAsync(entity, cancellationToken);
    public Task UpdateAsync(DiningAreaTable entity, CancellationToken cancellationToken = default)
    {
        context.Set<DiningAreaTable>().Update(entity);
        return Task.CompletedTask;
    }
}