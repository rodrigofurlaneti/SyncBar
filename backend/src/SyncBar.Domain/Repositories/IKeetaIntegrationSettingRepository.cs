using SyncBar.Domain.Entities;
namespace SyncBar.Domain.Repositories
{
    public interface IKeetaIntegrationSettingRepository
    {
        Task<KeetaIntegrationSetting?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<KeetaIntegrationSetting?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
        Task<KeetaIntegrationSetting?> GetByCompanyIdAsync(long companyId, CancellationToken cancellationToken = default);
        Task<KeetaIntegrationSetting?> GetByCompanyIdForUpdateAsync(long companyId, CancellationToken cancellationToken = default);
        Task<KeetaIntegrationSetting?> GetByBranchIdAsync(long branchId, CancellationToken cancellationToken = default);
        Task<KeetaIntegrationSetting?> GetByBranchIdForUpdateAsync(long branchId, CancellationToken cancellationToken = default);
        Task<KeetaIntegrationSetting?> GetByBranchOrCompanyFallbackAsync(long companyId, long? branchId, CancellationToken cancellationToken = default);
        Task<KeetaIntegrationSetting?> GetByScopeAsync(long companyId, long? branchId, CancellationToken cancellationToken = default);
        Task<bool> ExistsForCompanyAsync(long companyId, CancellationToken cancellationToken = default);
        Task<bool> ExistsForBranchAsync(long branchId, CancellationToken cancellationToken = default);
        Task AddAsync(KeetaIntegrationSetting setting, CancellationToken cancellationToken = default);
        void Update(KeetaIntegrationSetting setting);
        void Delete(KeetaIntegrationSetting setting);
    }
}
