namespace SyncBar.Application.Features.Integrations.IFood.Orders;

public sealed record IFoodOrderResponse(
    long Id,
    long CustomerOrderId,
    string IFoodOrderId,
    string? DisplayId,
    string IFoodOrderType,
    string Status,
    DateTime ConfirmDeadlineAt,
    DateTime? ConfirmedAt,
    bool HasUnmappedItems,
    string CustomerName,
    string? CustomerPhone,
    string? DeliveryAddress,
    decimal TotalAmount,
    DateTime CreatedAt);
