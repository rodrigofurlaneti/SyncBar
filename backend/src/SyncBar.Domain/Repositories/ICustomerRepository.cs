using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Customer?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Customer>> GetByCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Customer>> SearchAsync(long companyId, string term, CancellationToken cancellationToken = default);
    Task AddAsync(Customer entity, CancellationToken cancellationToken = default);
}
