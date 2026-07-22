using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IPurchaseRepository
{
    Task<Purchase?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Purchase>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(Purchase entity, CancellationToken cancellationToken = default);
}
