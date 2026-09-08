namespace SyncBar.Application.Abstractions.Integrations.Ifood;

public interface IIfoodEventInbox
{
    Task EnqueueAsync(long companyId, string eventId, string payload, CancellationToken ct);
}

public sealed record IfoodWebhookReceipt(int StatusCode, string[]? MerchantIds = null);

public interface IIfoodWebhookReceiver
{
    Task<IfoodWebhookReceipt> ReceiveAsync(long companyId, byte[] body, string? signature, CancellationToken ct);
}
