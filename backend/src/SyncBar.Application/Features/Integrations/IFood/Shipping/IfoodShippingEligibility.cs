using SyncBar.Domain.Entities;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

internal static class IfoodShippingEligibility
{
    public static Result Validate(IfoodOrder order)
    {
        if (!order.IsActive || order.Status is IfoodOrderStatuses.Cancelled or IfoodOrderStatuses.Concluded
            or IfoodOrderStatuses.Delivered or IfoodOrderStatuses.CancellationRequested)
            return Result.Failure(new Error("IfoodShipping.OrderClosed", "Este pedido não está disponível para solicitar uma entrega."));
        if (!string.Equals(order.IfoodOrderType, "DELIVERY", StringComparison.OrdinalIgnoreCase))
            return Result.Failure(new Error("IfoodShipping.DeliveryRequired", "Entregadores só podem ser solicitados para pedidos de delivery."));
        if (!string.Equals(order.DeliveredBy, "MERCHANT", StringComparison.OrdinalIgnoreCase))
            return Result.Failure(new Error("IfoodShipping.OwnFleetRequired", "O pedido precisa estar configurado para entrega própria. Pedidos com logística iFood já têm alocação de entregador."));
        return Result.Success();
    }
}
