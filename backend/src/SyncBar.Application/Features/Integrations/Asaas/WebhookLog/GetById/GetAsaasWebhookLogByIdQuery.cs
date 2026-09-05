using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByAsaasPaymentId;
namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetById
{
    public sealed record GetAsaasWebhookLogByIdQuery(
        long Id,
        long CompanyId) : IQuery<AsaasWebhookLogResponse>;
}
