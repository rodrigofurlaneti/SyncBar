namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

public sealed record IfoodShippingQuoteResponse(
    string QuoteId,
    decimal GrossValue,
    decimal Discount,
    decimal NetValue,
    double DeliveryTimeMinMinutes,
    double DeliveryTimeMaxMinutes,
    int DistanceMeters,
    DateTime? ExpirationAt);
