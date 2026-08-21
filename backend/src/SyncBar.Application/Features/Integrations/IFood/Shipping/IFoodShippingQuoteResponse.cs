namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

public sealed record IFoodShippingQuoteResponse(
    string QuoteId,
    decimal GrossValue,
    decimal Discount,
    decimal NetValue,
    double DeliveryTimeMinMinutes,
    double DeliveryTimeMaxMinutes,
    int DistanceMeters,
    DateTime? ExpirationAt);
