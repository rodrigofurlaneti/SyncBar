using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByAsaasPaymentId;
namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByIdForUpdate
{
    public sealed record GetAsaasWebhookLogByIdForUpdateQuery(
        long Id,
        long CompanyId) : IQuery<AsaasWebhookLogResponse>;
}
