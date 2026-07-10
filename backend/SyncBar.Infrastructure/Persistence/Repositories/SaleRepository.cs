using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class SaleRepository(AppDbContext context) : ISaleRepository
{
    public async Task<Sale?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.Sales.AsNoTracking()
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Sale>> GetByCashSessionAsync(long cashSessionId, CancellationToken cancellationToken = default)
        => await context.Sales.AsNoTracking()
            .Include(x => x.Payments)
            .Where(x => x.CashSessionId == cashSessionId && x.IsActive)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Sale>> GetByBranchAndPeriodAsync(
        long branchId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        => await context.Sales.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive && x.SoldAt >= from && x.SoldAt < to)
            .ToListAsync(cancellationToken);

    // Sequencial por filial — nunca IDENTITY global.
    public async Task<long> GetNextSaleNumberAsync(long branchId, CancellationToken cancellationToken = default)
    {
        var max = await context.Sales.AsNoTracking()
            .Where(x => x.BranchId == branchId)
            .MaxAsync(x => (long?)x.SaleNumber, cancellationToken);
        return (max ?? 0) + 1;
    }

    public async Task<bool> ExistsActiveByOrderAsync(long customerOrderId, CancellationToken cancellationToken = default)
        => await context.Sales.AsNoTracking()
            .AnyAsync(x => x.CustomerOrderId == customerOrderId && x.IsActive, cancellationToken);

    public async Task AddAsync(Sale entity, CancellationToken cancellationToken = default)
        => await context.Sales.AddAsync(entity, cancellationToken);
}
