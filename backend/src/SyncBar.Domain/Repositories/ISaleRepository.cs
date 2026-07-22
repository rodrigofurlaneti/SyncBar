using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Sale?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Sale>> GetByCashSessionAsync(long cashSessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Sale>> GetByBranchAndPeriodAsync(long branchId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<long> GetNextSaleNumberAsync(long branchId, CancellationToken cancellationToken = default);
    // Espelha UQ_Sale_CustomerOrderId (filtrado por IsActive = 1).
    Task<bool> ExistsActiveByOrderAsync(long customerOrderId, CancellationToken cancellationToken = default);
    Task AddAsync(Sale entity, CancellationToken cancellationToken = default);
}
