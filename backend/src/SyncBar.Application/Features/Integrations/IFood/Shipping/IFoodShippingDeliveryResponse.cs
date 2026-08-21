namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

public sealed record IFoodShippingDeliveryResponse(
    long Id,
    string? OrderReference,
    string CustomerName,
    string DeliveryAddress,
    decimal MerchantFee,
    string Status,
    string? TrackingUrl,
    DateTime RequestedAt,
    DateTime? CancelledAt);
