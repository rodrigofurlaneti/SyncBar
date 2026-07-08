using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IStockMovementRepository
{
    Task<IReadOnlyCollection<StockMovement>> GetByStockItemAsync(long stockItemId, CancellationToken cancellationToken = default);
    Task AddAsync(StockMovement entity, CancellationToken cancellationToken = default);
}
