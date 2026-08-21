using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IFoodOrderRepository(AppDbContext context) : IIFoodOrderRepository
{
    public async Task<IFoodOrder?> GetByIFoodOrderIdAsync(string ifoodOrderId, CancellationToken cancellationToken = default)
        => await context.IFoodOrders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IFoodOrderId == ifoodOrderId, cancellationToken);

    public async Task<IFoodOrder?> GetByIFoodOrderIdForUpdateAsync(string ifoodOrderId, CancellationToken cancellationToken = default)
        => await context.IFoodOrders
            .FirstOrDefaultAsync(x => x.IFoodOrderId == ifoodOrderId, cancellationToken);

    public async Task<IFoodOrder?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.IFoodOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<IFoodOrder>> GetOpenByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IFoodOrders.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive
                && x.Status != IFoodOrderStatuses.Concluded && x.Status != IFoodOrderStatuses.Cancelled)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(IFoodOrder entity, CancellationToken cancellationToken = default)
        => await context.IFoodOrders.AddAsync(entity, cancellationToken);
}
