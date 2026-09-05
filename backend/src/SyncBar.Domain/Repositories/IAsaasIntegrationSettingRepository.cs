using SyncBar.Domain.Entities;

namespace SyncBar.Domain.Repositories;

public interface IAsaasIntegrationSettingRepository
{
    Task<AsaasIntegrationSetting?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<AsaasIntegrationSetting?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<AsaasIntegrationSetting?> GetByCompanyIdAsync(long companyId, CancellationToken cancellationToken = default);
    Task<AsaasIntegrationSetting?> GetByCompanyIdForUpdateAsync(long companyId, CancellationToken cancellationToken = default);
    Task<AsaasIntegrationSetting?> GetByBranchIdAsync(long branchId, CancellationToken cancellationToken = default);
    Task<AsaasIntegrationSetting?> GetByBranchIdForUpdateAsync(long branchId, CancellationToken cancellationToken = default);
    Task<AsaasIntegrationSetting?> GetByBranchOrCompanyFallbackAsync(long companyId, long? branchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AsaasIntegrationSetting>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsForCompanyAsync(long companyId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForBranchAsync(long branchId, CancellationToken cancellationToken = default);
    Task AddAsync(AsaasIntegrationSetting setting, CancellationToken cancellationToken = default);
    void Update(AsaasIntegrationSetting setting);
    void Delete(AsaasIntegrationSetting setting);
}