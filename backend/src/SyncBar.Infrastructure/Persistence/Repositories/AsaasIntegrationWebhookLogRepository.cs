using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Enums;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Persistence.Repositories;

internal sealed class AsaasIntegrationWebhookLogRepository(AppDbContext context) : IAsaasIntegrationWebhookLogRepository
{
    public async Task<AsaasIntegrationWebhookLog?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await context.Set<AsaasIntegrationWebhookLog>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

    public async Task<IReadOnlyList<AsaasIntegrationWebhookLog>> GetByPaymentIdAsync(
        long companyId,
        string paymentId,
        CancellationToken cancellationToken = default)
        => await context.Set<AsaasIntegrationWebhookLog>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.PaymentId == paymentId && x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AsaasIntegrationWebhookLog>> GetUnprocessedLogsAsync(
        long companyId,
        int limit = 50,
        CancellationToken cancellationToken = default)
        => await context.Set<AsaasIntegrationWebhookLog>()
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Status == WebhookLogStatus.Pending && x.IsActive)
            .OrderBy(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsByEventIdAsync(
        string asaasEventId,
        CancellationToken cancellationToken = default)
        => await context.Set<AsaasIntegrationWebhookLog>()
            .AnyAsync(x => x.AsaasEventId == asaasEventId && x.IsActive, cancellationToken);

    public async Task<bool> HasAlreadyProcessedEventAsync(
        string asaasEventId,
        CancellationToken cancellationToken = default)
        => await context.Set<AsaasIntegrationWebhookLog>()
            .AnyAsync(x => x.AsaasEventId == asaasEventId &&
                           x.Status == WebhookLogStatus.Processed &&
                           x.IsActive, cancellationToken);

    public async Task<AsaasIntegrationWebhookLog?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default)
        => await context.Set<AsaasIntegrationWebhookLog>()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

    public async Task AddAsync(AsaasIntegrationWebhookLog webhookLog, CancellationToken cancellationToken = default)
        => await context.Set<AsaasIntegrationWebhookLog>().AddAsync(webhookLog, cancellationToken);

    public void Update(AsaasIntegrationWebhookLog webhookLog)
        => context.Set<AsaasIntegrationWebhookLog>().Update(webhookLog);

    public void Delete(AsaasIntegrationWebhookLog webhookLog)
        => context.Set<AsaasIntegrationWebhookLog>().Remove(webhookLog);
}