using SyncBar.Domain.Entities;
namespace SyncBar.Domain.Repositories
{
    public interface IAsaasIntegrationSavedCardRepository
    {
        Task<AsaasIntegrationSavedCard?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<AsaasIntegrationSavedCard?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AsaasIntegrationSavedCard>> GetByCustomerIdAsync(long customerId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AsaasIntegrationSavedCard>> GetByCustomerIdAndCompanyIdForUpdateAsync(long customerId, long companyId, CancellationToken cancellationToken = default);
        Task<AsaasIntegrationSavedCard?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<bool> ExistsByTokenAsync(string token, CancellationToken cancellationToken = default);
        Task AddAsync(AsaasIntegrationSavedCard card, CancellationToken cancellationToken = default);
        void Update(AsaasIntegrationSavedCard card);
        void Delete(AsaasIntegrationSavedCard card);
    }
}
