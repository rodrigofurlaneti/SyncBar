using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IIFoodComplementGroupMappingRepository
{
    Task<IFoodComplementGroupMapping?> GetByComplementGroupAndBranchAsync(long complementGroupId, long branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<IFoodComplementGroupMapping>> GetByBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(IFoodComplementGroupMapping entity, CancellationToken cancellationToken = default);
}
