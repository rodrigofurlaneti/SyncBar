namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

public sealed record IfoodShippingDeliveryResponse(
    long Id,
    string? OrderReference,
    string CustomerName,
    string DeliveryAddress,
    decimal MerchantFee,
    string Status,
    string? TrackingUrl,
    DateTime RequestedAt,
    DateTime? CancelledAt);
