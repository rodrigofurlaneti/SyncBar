using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class OrderItem : Entity
{
    public long CustomerOrderId { get; private set; }
    public long ProductId { get; private set; }
    public long OrderItemStatusId { get; private set; }
    public long? EmployeeId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? SentToKitchenAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public long? CancelledByEmployeeId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private OrderItem() : base(0) { }

    private OrderItem(long customerOrderId, long productId, decimal unitPrice, decimal quantity, string? notes, long? employeeId, DateTime utcNow) : base(0)
    {
        CustomerOrderId = customerOrderId;
        ProductId = productId;
        UnitPrice = unitPrice;
        Quantity = quantity;
        Notes = notes;
        EmployeeId = employeeId;
        OrderItemStatusId = OrderItemStatusIds.Lancado;
        TotalAmount = Math.Round(unitPrice * quantity, 2);
        IsActive = true;
        CreatedAt = utcNow;
    }

    internal static Result<OrderItem> Create(long customerOrderId, long productId, decimal unitPrice, decimal quantity, string? notes, long? employeeId, DateTime utcNow)
    {
        if (quantity <= 0)
            return Result.Failure<OrderItem>(new Error("OrderItem.InvalidQuantity", "Quantity must be greater than zero."));
        if (unitPrice < 0)
            return Result.Failure<OrderItem>(new Error("OrderItem.InvalidUnitPrice", "Unit price cannot be negative."));

        return Result.Success(new OrderItem(customerOrderId, productId, unitPrice, quantity, notes, employeeId, utcNow));
    }

    internal Result UpdateStatus(long orderItemStatusId, long? actorEmployeeId, DateTime utcNow)
    {
        if (OrderItemStatusId is OrderItemStatusIds.Entregue or OrderItemStatusIds.Cancelado)
            return Result.Failure(new Error("OrderItem.FinalStatus", "Delivered or cancelled items cannot change status."));

        OrderItemStatusId = orderItemStatusId;
        if (orderItemStatusId == OrderItemStatusIds.EnviadoCozinha) SentToKitchenAt = utcNow;
        if (orderItemStatusId == OrderItemStatusIds.Entregue) DeliveredAt = utcNow;
        if (orderItemStatusId == OrderItemStatusIds.Cancelado) CancelledByEmployeeId = actorEmployeeId;

        UpdatedAt = utcNow;
        return Result.Success();
    }

    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        UpdatedAt = utcNow;
    }
}