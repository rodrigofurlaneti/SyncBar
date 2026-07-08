using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class StockMovementRepository(AppDbContext context) : IStockMovementRepository
{
    public async Task<IReadOnlyCollection<StockMovement>> GetByStockItemAsync(long stockItemId, CancellationToken cancellationToken = default)
        => await context.StockMovements.AsNoTracking()
            .Where(x => x.StockItemId == stockItemId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(StockMovement entity, CancellationToken cancellationToken = default)
        => await context.StockMovements.AddAsync(entity, cancellationToken);
}
