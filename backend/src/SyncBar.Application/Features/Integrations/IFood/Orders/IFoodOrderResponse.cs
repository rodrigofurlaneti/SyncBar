namespace SyncBar.Application.Features.Integrations.IFood.Orders;

public sealed record IFoodOrderResponse(
    long Id,
    long CustomerOrderId,
    string IFoodOrderId,
    string? DisplayId,
    string IFoodOrderType,
    // Bruto do iFood (delivery.deliveredBy) — "IFOOD" = logística do próprio iFood; qualquer
    // outro valor (ex.: "MERCHANT") = self-delivery/frota própria, elegível pra tela de
    // Logística (fase 7). Nulo para TAKEOUT/DINE_IN ou quando o iFood não informou o campo.
    string? DeliveredBy,
    string Status,
    DateTime ConfirmDeadlineAt,
    DateTime? ConfirmedAt,
    bool HasUnmappedItems,
    string CustomerName,
    string? CustomerPhone,
    string? DeliveryAddress,
    decimal TotalAmount,
    DateTime CreatedAt);
