using SyncBar.Domain.Entities;
namespace SyncBar.Domain.Repositories
{
    public interface IAsaasIntegrationCustomerRepository
    {
        Task<AsaasIntegrationCustomer?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<AsaasIntegrationCustomer?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
        Task<AsaasIntegrationCustomer?> GetByCustomerIdAndCompanyIdAsync(long customerId, long companyId, CancellationToken cancellationToken = default);
        Task<AsaasIntegrationCustomer?> GetByCustomerIdAndCompanyIdForUpdateAsync(long customerId, long companyId, CancellationToken cancellationToken = default);
        Task<AsaasIntegrationCustomer?> GetByAsaasCustomerIdAsync(string asaasCustomerId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AsaasIntegrationCustomer>> GetAllByCompanyIdAsync(long companyId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(long customerId, long companyId, CancellationToken cancellationToken = default);
        Task AddAsync(AsaasIntegrationCustomer customer, CancellationToken cancellationToken = default);
        void Update(AsaasIntegrationCustomer customer);
        void Delete(AsaasIntegrationCustomer customer);
    }
}
