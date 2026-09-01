using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIfoodComplementGroupMappingRepository
{
    Task<IfoodComplementGroupMapping?> GetByComplementGroupAndBranchAsync(long complementGroupId, long branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<IfoodComplementGroupMapping>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(IfoodComplementGroupMapping entity, CancellationToken cancellationToken = default);
}
