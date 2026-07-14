using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Supplier?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Supplier>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    Task AddAsync(Supplier entity, CancellationToken cancellationToken = default);
}
