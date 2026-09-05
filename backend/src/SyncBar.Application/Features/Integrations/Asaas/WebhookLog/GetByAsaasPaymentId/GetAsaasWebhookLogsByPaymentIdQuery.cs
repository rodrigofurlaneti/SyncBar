using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByAsaasPaymentId
{
    public sealed record GetAsaasWebhookLogsByPaymentIdQuery(
        long CompanyId,
        string PaymentId) : IQuery<IReadOnlyList<AsaasWebhookLogResponse>>;
}
