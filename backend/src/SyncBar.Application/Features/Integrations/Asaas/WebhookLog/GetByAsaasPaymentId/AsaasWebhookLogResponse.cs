namespace SyncBar.Application.Features.Integrations.Asaas.WebhookLog.GetByAsaasPaymentId
{
    public sealed record AsaasWebhookLogResponse(
        long Id,
        long CompanyId,
        long? BranchId,
        string Event,
        string? AsaasEventId,
        string? PaymentId,
        string Payload,
        string? RequestHeaders,
        string? IpAddress,
        string Status,
        string? ErrorMessage,
        DateTime CreatedAt,
        DateTime? ProcessedAt);
}
