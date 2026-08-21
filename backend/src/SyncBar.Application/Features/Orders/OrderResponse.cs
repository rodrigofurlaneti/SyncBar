namespace SyncBar.Application.Features.Orders;

// ComplementItemName não é resolvido aqui (OrderItemComplement só conhece o ComplementId,
// cross-aggregate) — o front-end casa pelo Id usando o catálogo de complementos já carregado
// (GetComplementGroups), mesma ideia de ProductId em OrderItemResponse não carregar o nome do produto.
public sealed record OrderItemComplementResponse(long Id, long ComplementId, decimal UnitPriceCharged);

public sealed record OrderItemResponse(
    long Id,
    long ProductId,
    long OrderItemStatusId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal TotalAmount,
    string? Notes,
    IReadOnlyCollection<OrderItemComplementResponse> Complements);

public sealed record OrderResponse(
    long Id,
    long BranchId,
    long? DiningTableId,
    long? ComandaId,
    long EmployeeId,
    long OrderStatusId,
    int? GuestCount,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal ServiceFeeAmount,
    decimal TotalAmount,
    decimal PartialPaidAmount,
    decimal? CreditLimitAmount,
    string? Notes,
    long OrderTypeId,
    string? CustomerName,
    string? CustomerPhone,
    string? DeliveryAddress,
    IReadOnlyCollection<OrderItemResponse> Items);
