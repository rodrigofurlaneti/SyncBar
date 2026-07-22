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

    private OrderItem(long customerOrderId, long productId, decimal unitPrice, decimal quantity, string? notes, long? employeeId) : base(0)
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
        CreatedAt = DateTime.UtcNow;
    }

    internal static Result<OrderItem> Create(long customerOrderId, long productId, decimal unitPrice, decimal quantity, string? notes, long? employeeId)
    {
        if (quantity <= 0)
            return Result.Failure<OrderItem>(new Error("OrderItem.InvalidQuantity", "Quantity must be greater than zero."));
        if (unitPrice < 0)
            return Result.Failure<OrderItem>(new Error("OrderItem.InvalidUnitPrice", "Unit price cannot be negative."));

        return Result.Success(new OrderItem(customerOrderId, productId, unitPrice, quantity, notes, employeeId));
    }

    internal Result UpdateStatus(long orderItemStatusId, long? actorEmployeeId = null)
    {
        if (OrderItemStatusId is OrderItemStatusIds.Entregue or OrderItemStatusIds.Cancelado)
            return Result.Failure(new Error("OrderItem.FinalStatus", "Delivered or cancelled items cannot change status."));

        OrderItemStatusId = orderItemStatusId;
        if (orderItemStatusId == OrderItemStatusIds.EnviadoCozinha) SentToKitchenAt = DateTime.UtcNow;
        if (orderItemStatusId == OrderItemStatusIds.Entregue) DeliveredAt = DateTime.UtcNow;
        if (orderItemStatusId == OrderItemStatusIds.Cancelado) CancelledByEmployeeId = actorEmployeeId;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
