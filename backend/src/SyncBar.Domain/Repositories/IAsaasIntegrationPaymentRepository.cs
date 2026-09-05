using SyncBar.Domain.Entities;
namespace SyncBar.Domain.Repositories
{
    public interface IAsaasIntegrationPaymentRepository
    {
        Task<AsaasIntegrationPayment?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<AsaasIntegrationPayment?> GetByAsaasPaymentIdAsync(string asaasPaymentId, CancellationToken cancellationToken = default);
        Task<AsaasIntegrationPayment?> GetByCustomerOrderIdAsync(long customerOrderId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AsaasIntegrationPayment>> GetByBranchIdAsync(long branchId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AsaasIntegrationPayment>> GetPendingByBranchIdAsync(long branchId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByAsaasPaymentIdAsync(string asaasPaymentId, CancellationToken cancellationToken = default);
        Task<AsaasIntegrationPayment?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
        Task<AsaasIntegrationPayment?> GetByAsaasPaymentIdForUpdateAsync(string asaasPaymentId, CancellationToken cancellationToken = default);
        Task AddAsync(AsaasIntegrationPayment payment, CancellationToken cancellationToken = default);
        void Update(AsaasIntegrationPayment payment);
        void Delete(AsaasIntegrationPayment payment);
    }
}
