using SyncBar.Domain.Entities;
namespace SyncBar.Domain.Repositories
{
    public interface IKeetaIntegrationRefundDisputeRepository
    {
        Task<KeetaIntegrationRefundDispute?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<KeetaIntegrationRefundDispute?> GetByAfterSaleOrderIdAsync(long afterSaleOrderId, CancellationToken cancellationToken = default);
        Task<KeetaIntegrationRefundDispute?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<KeetaIntegrationRefundDispute>> GetPendingDisputesByBranchAsync(long branchId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByAfterSaleOrderIdAsync(long afterSaleOrderId, CancellationToken cancellationToken = default);
        Task AddAsync(KeetaIntegrationRefundDispute dispute, CancellationToken cancellationToken = default);
        void Update(KeetaIntegrationRefundDispute dispute);
        void Delete(KeetaIntegrationRefundDispute dispute);
    }
}
