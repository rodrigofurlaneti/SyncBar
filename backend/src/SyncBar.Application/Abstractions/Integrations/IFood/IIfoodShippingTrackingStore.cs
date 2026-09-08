namespace SyncBar.Application.Abstractions.Integrations.Ifood;

public interface IIfoodShippingTrackingStore
{
    Task<bool> ApplyEventAsync(long companyId, IfoodPollingEvent evt, CancellationToken ct);
    Task<IfoodShippingTrackingResult> ReadAsync(long companyId, string orderId, CancellationToken ct);
}
