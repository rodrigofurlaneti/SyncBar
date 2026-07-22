using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IBranchRepository
{
    Task<Branch?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Branch?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Branch>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    Task AddAsync(Branch entity, CancellationToken cancellationToken = default);
}
