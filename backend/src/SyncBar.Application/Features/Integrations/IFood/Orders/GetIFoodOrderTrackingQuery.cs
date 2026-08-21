using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

// Fase 9b — rastreamento (posição do entregador) de um pedido que veio do iFood. Mesmo shape de
// resposta usado em GetIFoodShippingTrackingQuery (Fase 8), mas aqui a fonte é
// GET order/v1.0/orders/{id}/tracking em vez do módulo Shipping.
public sealed record IFoodOrderTrackingResponse(
    double? Latitude, double? Longitude, DateTime? ExpectedDelivery, double? DeliveryEtaEndMinutes, double? PickupEtaStartMinutes);

public sealed record GetIFoodOrderTrackingQuery(long IFoodOrderId) : IQuery<IFoodOrderTrackingResponse>;
