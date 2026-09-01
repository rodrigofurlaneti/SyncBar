namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

public sealed record IfoodOrderResponse(
    long Id,
    long CustomerOrderId,
    string IfoodOrderId,
    string? DisplayId,
    string IfoodOrderType,
    // Bruto do Ifood (delivery.deliveredBy) — "Ifood" = logística do próprio Ifood; qualquer
    // outro valor (ex.: "MERCHANT") = self-delivery/frota própria, elegível pra tela de
    // Logística (fase 7). Nulo para TAKEOUT/DINE_IN ou quando o Ifood não informou o campo.
    string? DeliveredBy,
    // Fase 14 — "IMMEDIATE" ou "SCHEDULED"; PreparationStartDateTime só preenchido quando
    // agendado. Usado pela tela de Pedidos pra mostrar "Agendado para HH:mm" em vez de tratar
    // todo pedido como imediato.
    string OrderTiming,
    DateTime? PreparationStartDateTime,
    string Status,
    DateTime ConfirmDeadlineAt,
    DateTime? ConfirmedAt,
    bool HasUnmappedItems,
    string CustomerName,
    string? CustomerPhone,
    string? DeliveryAddress,
    decimal TotalAmount,
    DateTime CreatedAt);
