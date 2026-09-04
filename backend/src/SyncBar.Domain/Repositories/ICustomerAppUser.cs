using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface ICustomerAppUserRepository
{
    Task<IEnumerable<CustomerAppUser?>> GetByCustomerId(long customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerAppUser?>> GetByBranchId(long branchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerAppUser?>> GetByCompanyId(long companyId, CancellationToken cancellationToken = default);
    Task<CustomerAppUser?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task AddAsync(CustomerAppUser entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(CustomerAppUser entity, CancellationToken cancellationToken = default);
    Task RemoveAsync(long id, CancellationToken cancellationToken = default);
}
