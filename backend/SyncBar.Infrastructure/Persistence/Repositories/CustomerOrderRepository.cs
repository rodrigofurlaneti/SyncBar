using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class CustomerOrderRepository(AppDbContext context) : ICustomerOrderRepository
{
    private static readonly long[] OpenStatuses =
        [OrderStatusIds.Aberto, OrderStatusIds.EmAndamento, OrderStatusIds.AguardandoPagamento];

    public async Task<CustomerOrder?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.CustomerOrders.AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    // Tracked, com itens — para AddItem/Close/Cancel.
    public async Task<CustomerOrder?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.CustomerOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<CustomerOrder>> GetOpenByBranchAsync(long branchId, CancellationToken cancellationToken = default)
        => await context.CustomerOrders.AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.BranchId == branchId && x.IsActive && OpenStatuses.Contains(x.OrderStatusId))
            .ToListAsync(cancellationToken);

    public async Task<bool> HasOpenOrderForTableAsync(long diningTableId, CancellationToken cancellationToken = default)
        => await context.CustomerOrders.AsNoTracking()
            .AnyAsync(x => x.DiningTableId == diningTableId && x.IsActive && OpenStatuses.Contains(x.OrderStatusId), cancellationToken);

    public async Task<bool> HasOpenOrderForComandaAsync(long comandaId, CancellationToken cancellationToken = default)
        => await context.CustomerOrders.AsNoTracking()
            .AnyAsync(x => x.ComandaId == comandaId && x.IsActive && OpenStatuses.Contains(x.OrderStatusId), cancellationToken);

    public async Task AddAsync(CustomerOrder entity, CancellationToken cancellationToken = default)
        => await context.CustomerOrders.AddAsync(entity, cancellationToken);
}
