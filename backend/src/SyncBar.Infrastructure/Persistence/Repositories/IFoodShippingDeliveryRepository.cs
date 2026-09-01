using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IfoodShippingDeliveryRepository(AppDbContext context) : IIfoodShippingDeliveryRepository
{
    public async Task<IfoodShippingDelivery?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.IfoodShippingDeliveries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IfoodShippingDelivery?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.IfoodShippingDeliveries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<IfoodShippingDelivery>> GetOpenByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IfoodShippingDeliveries.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive && x.Status != IfoodShippingStatuses.Cancelled)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(IfoodShippingDelivery entity, CancellationToken cancellationToken = default)
        => await context.IfoodShippingDeliveries.AddAsync(entity, cancellationToken);
}
