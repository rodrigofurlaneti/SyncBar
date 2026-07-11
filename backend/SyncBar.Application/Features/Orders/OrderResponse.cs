namespace SyncBar.Application.Features.Orders;

public sealed record OrderItemResponse(
    long Id,
    long ProductId,
    long OrderItemStatusId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal TotalAmount,
    string? Notes);

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
    string? Notes,
    IReadOnlyCollection<OrderItemResponse> Items);
