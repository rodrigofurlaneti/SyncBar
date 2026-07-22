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

    public async Task<decimal> GetSaleCostAsync(long branchId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        => await context.StockMovements.AsNoTracking()
            .Join(context.StockItems, m => m.StockItemId, i => i.Id, (m, i) => new { m, i })
            .Where(x => x.i.BranchId == branchId && x.m.IsActive
                && x.m.StockMovementTypeId == SyncBar.Domain.Constants.StockMovementTypeIds.SaidaVenda
                && x.m.MovedAt >= from && x.m.MovedAt < to)
            .SumAsync(x => x.m.TotalCost ?? 0, cancellationToken);

    public async Task<IReadOnlyCollection<ProductQuantity>> GetSaleQuantitiesByProductAsync(
        long branchId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        => await context.StockMovements.AsNoTracking()
            .Join(context.StockItems, m => m.StockItemId, i => i.Id, (m, i) => new { m, i })
            .Where(x => x.i.BranchId == branchId && x.m.IsActive
                && x.m.StockMovementTypeId == SyncBar.Domain.Constants.StockMovementTypeIds.SaidaVenda
                && x.m.MovedAt >= from && x.m.MovedAt < to)
            .GroupBy(x => x.i.ProductId)
            .Select(g => new ProductQuantity(g.Key, g.Sum(x => x.m.Quantity)))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(StockMovement entity, CancellationToken cancellationToken = default)
        => await context.StockMovements.AddAsync(entity, cancellationToken);
}
