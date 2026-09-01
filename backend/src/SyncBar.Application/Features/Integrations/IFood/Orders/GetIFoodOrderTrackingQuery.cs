using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

// Fase 9b — rastreamento (posição do entregador) de um pedido que veio do Ifood. Mesmo shape de
// resposta usado em GetIfoodShippingTrackingQuery (Fase 8), mas aqui a fonte é
// GET order/v1.0/orders/{id}/tracking em vez do módulo Shipping.
public sealed record IfoodOrderTrackingResponse(
    double? Latitude, double? Longitude, DateTime? ExpectedDelivery, double? DeliveryEtaEndMinutes, double? PickupEtaStartMinutes);

public sealed record GetIfoodOrderTrackingQuery(long IfoodOrderId) : IQuery<IfoodOrderTrackingResponse>;
