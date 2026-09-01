using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IfoodOrderRepository(AppDbContext context) : IIfoodOrderRepository
{
    public async Task<IfoodOrder?> GetByIfoodOrderIdAsync(string IfoodOrderId, CancellationToken cancellationToken = default)
        => await context.IfoodOrders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IfoodOrderId == IfoodOrderId, cancellationToken);

    public async Task<IfoodOrder?> GetByIfoodOrderIdForUpdateAsync(string IfoodOrderId, CancellationToken cancellationToken = default)
        => await context.IfoodOrders
            .FirstOrDefaultAsync(x => x.IfoodOrderId == IfoodOrderId, cancellationToken);

    public async Task<IfoodOrder?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.IfoodOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<IfoodOrder>> GetOpenByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IfoodOrders.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive
                && x.Status != IfoodOrderStatuses.Concluded && x.Status != IfoodOrderStatuses.Cancelled)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(IfoodOrder entity, CancellationToken cancellationToken = default)
        => await context.IfoodOrders.AddAsync(entity, cancellationToken);
}
