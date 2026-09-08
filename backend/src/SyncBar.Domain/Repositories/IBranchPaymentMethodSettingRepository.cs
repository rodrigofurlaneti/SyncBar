using SyncBar.Domain.Entities;
namespace SyncBar.Domain.Repositories
{
    public interface IBranchPaymentMethodSettingRepository
    {
        Task<BranchPaymentMethodSetting?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<BranchPaymentMethodSetting?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
        Task<BranchPaymentMethodSetting?> GetByCompanyIdAsync(long companyId, CancellationToken cancellationToken = default);
        Task<BranchPaymentMethodSetting?> GetByCompanyIdForUpdateAsync(long companyId, CancellationToken cancellationToken = default);
        Task<BranchPaymentMethodSetting?> GetByBranchIdAsync(long branchId, CancellationToken cancellationToken = default);
        Task<BranchPaymentMethodSetting?> GetByBranchIdForUpdateAsync(long branchId, CancellationToken cancellationToken = default);
        Task<BranchPaymentMethodSetting?> GetByBranchOrCompanyFallbackAsync(long companyId, long? branchId, CancellationToken cancellationToken = default);
        Task<BranchPaymentMethodSetting?> GetByScopeAsync(long companyId, long? branchId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BranchPaymentMethodSetting>> GetAllActiveAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<BranchPaymentMethodSetting>> GetAllActiveByCompanyIdAsync(long companyId, CancellationToken cancellationToken = default);
        Task<bool> ExistsForCompanyAsync(long companyId, CancellationToken cancellationToken = default);
        Task<bool> ExistsForBranchAsync(long branchId, CancellationToken cancellationToken = default);
        Task AddAsync(BranchPaymentMethodSetting setting, CancellationToken cancellationToken = default);
        void Update(BranchPaymentMethodSetting setting);
        void Delete(BranchPaymentMethodSetting setting);
    }
}
