using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IComandaSettingRepository
{
    Task<ComandaSetting?> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task<ComandaSetting?> GetByBranchForUpdateAsync(long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(ComandaSetting entity, CancellationToken cancellationToken = default);
}
