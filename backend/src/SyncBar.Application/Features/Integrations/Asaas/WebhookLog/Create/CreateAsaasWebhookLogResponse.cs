namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Create
{
    public sealed record CreateAsaasWebhookLogResponse(
       long Id,
       string Event,
       string? PaymentId,
       DateTime ReceivedAt);
}
