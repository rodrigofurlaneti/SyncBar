using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Create
{
    public sealed record CreateAsaasWebhookLogCommand(
        long CompanyId,
        long? BranchId,
        string Event,
        string? AsaasEventId,
        string? PaymentId,
        string Payload,
        string? RequestHeaders,
        string? IpAddress) : ICommand<CreateAsaasWebhookLogResponse>;
}
