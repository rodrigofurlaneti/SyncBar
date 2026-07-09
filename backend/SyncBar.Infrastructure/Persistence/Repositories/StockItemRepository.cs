using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class StockItemRepository(AppDbContext context) : IStockItemRepository
{
    public async Task<StockItem?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.StockItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<StockItem?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.StockItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<StockItem>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.StockItems.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive)
            .ToListAsync(cancellationToken);

    // Tracked — saldo e alterado junto com o StockMovement correspondente.
    public async Task<StockItem?> GetByBranchAndProductForUpdateAsync(long branchId, long productId, CancellationToken cancellationToken = default)
        => await context.StockItems
            .FirstOrDefaultAsync(x => x.BranchId == branchId && x.ProductId == productId && x.IsActive, cancellationToken);

    public async Task<IReadOnlyCollection<StockItem>> GetBelowMinimumAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.StockItems.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive && x.CurrentQuantity < x.MinimumQuantity)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(StockItem entity, CancellationToken cancellationToken = default)
        => await context.StockItems.AddAsync(entity, cancellationToken);
}
