using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IOperatingCostRepository
{
    Task<OperatingCost?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<OperatingCost>> GetByBranchAndMonthAsync(
        long branchId, int referenceYear, int referenceMonth, CancellationToken cancellationToken = default);
    Task AddAsync(OperatingCost entity, CancellationToken cancellationToken = default);
}
