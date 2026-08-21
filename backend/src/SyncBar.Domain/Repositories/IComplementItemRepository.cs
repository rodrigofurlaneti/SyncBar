using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IComplementItemRepository
{
    Task<ComplementItem?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ComplementItem?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ComplementItem>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ComplementItem>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
    Task AddAsync(ComplementItem entity, CancellationToken cancellationToken = default);
}
