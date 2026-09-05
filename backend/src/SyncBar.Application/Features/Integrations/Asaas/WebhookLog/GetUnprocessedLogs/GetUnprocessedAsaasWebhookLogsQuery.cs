using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByAsaasPaymentId;
namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetUnprocessedLogs
{
    public sealed record GetUnprocessedAsaasWebhookLogsQuery(
        long CompanyId,
        int Limit = 50) : IQuery<IReadOnlyList<AsaasWebhookLogResponse>>;
}
