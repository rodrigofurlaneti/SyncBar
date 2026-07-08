using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IStockItemRepository
{
    Task<StockItem?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<StockItem?> GetByBranchAndProductForUpdateAsync(long branchId, long productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<StockItem>> GetBelowMinimumAsync(long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(StockItem entity, CancellationToken cancellationToken = default);
}
