using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class IFoodShippingDeliveryRepository(AppDbContext context) : IIFoodShippingDeliveryRepository
{
    public async Task<IFoodShippingDelivery?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.IFoodShippingDeliveries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IFoodShippingDelivery?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.IFoodShippingDeliveries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<IFoodShippingDelivery>> GetOpenByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.IFoodShippingDeliveries.AsNoTracking()
            .Where(x => x.BranchId == branchId && x.IsActive && x.Status != IFoodShippingStatuses.Cancelled)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(IFoodShippingDelivery entity, CancellationToken cancellationToken = default)
        => await context.IFoodShippingDeliveries.AddAsync(entity, cancellationToken);
}
