using SyncBar.Domain.Entities;
namespace SyncBar.Domain.Repositories
{
    public interface IKeetaIntegrationMerchantMappingRepository
    {
        Task<KeetaIntegrationMerchantMapping?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<KeetaIntegrationMerchantMapping?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
        Task<KeetaIntegrationMerchantMapping?> GetByKeetaMerchantIdAsync(long keetaMerchantId, CancellationToken cancellationToken = default);
        Task<KeetaIntegrationMerchantMapping?> GetByInternalMerchantIdAsync(string internalMerchantId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<KeetaIntegrationMerchantMapping>> GetAllByCompanyIdAsync(long companyId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<KeetaIntegrationMerchantMapping>> GetAllByBranchIdAsync(long branchId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByKeetaMerchantIdAsync(long keetaMerchantId, CancellationToken cancellationToken = default);
        Task AddAsync(KeetaIntegrationMerchantMapping mapping, CancellationToken cancellationToken = default);
        void Update(KeetaIntegrationMerchantMapping mapping);
        void Delete(KeetaIntegrationMerchantMapping mapping);
    }
}
