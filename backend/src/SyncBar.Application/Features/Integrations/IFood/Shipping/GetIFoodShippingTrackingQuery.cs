using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

public sealed record IFoodShippingTrackingResponse(
    double? Latitude, double? Longitude, DateTime? ExpectedDelivery, double? DeliveryEtaEndMinutes, double? PickupEtaStartMinutes);

// Id aqui é o Id LOCAL (long) do IFoodShippingDelivery — o handler resolve o IFoodDeliveryId
// (string do iFood) internamente antes de chamar o cliente HTTP.
public sealed record GetIFoodShippingTrackingQuery(long Id) : IQuery<IFoodShippingTrackingResponse>;
