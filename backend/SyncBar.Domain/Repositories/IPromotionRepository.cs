using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Promotion>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(Promotion entity, CancellationToken cancellationToken = default);
}
