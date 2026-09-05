using SyncBar.Domain.Entities;
namespace SyncBar.Domain.Repositories
{
    public interface IAsaasIntegrationWebhookLogRepository
    {
        Task<IEnumerable<AsaasIntegrationWebhookLog?>> GetAsync(CancellationToken cancellationToken = default);
        Task<AsaasIntegrationWebhookLog?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<AsaasIntegrationWebhookLog?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AsaasIntegrationWebhookLog>> GetByPaymentIdAsync(long companyId, string paymentId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AsaasIntegrationWebhookLog>> GetUnprocessedLogsAsync(long companyId, int limit = 50, CancellationToken cancellationToken = default);
        Task<bool> ExistsByEventIdAsync(string asaasEventId, CancellationToken cancellationToken = default);
        Task<bool> HasAlreadyProcessedEventAsync(string asaasEventId, CancellationToken cancellationToken = default);
        Task AddAsync(AsaasIntegrationWebhookLog webhookLog, CancellationToken cancellationToken = default);
        void Update(AsaasIntegrationWebhookLog webhookLog);
        void Delete(AsaasIntegrationWebhookLog webhookLog);
    }
}