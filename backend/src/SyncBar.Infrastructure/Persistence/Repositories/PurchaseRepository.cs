using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class PurchaseRepository(AppDbContext context) : IPurchaseRepository
{
    public async Task<Purchase?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.Purchases.AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Purchase>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.Purchases.AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.BranchId == branchId && x.IsActive)
            .OrderByDescending(x => x.PurchasedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Purchase entity, CancellationToken cancellationToken = default)
        => await context.Purchases.AddAsync(entity, cancellationToken);
}
