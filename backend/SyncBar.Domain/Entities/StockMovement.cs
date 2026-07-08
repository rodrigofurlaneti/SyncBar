using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class StockMovement : Entity
{
    public long StockItemId { get; private set; }
    public long StockMovementTypeId { get; private set; }
    public long? PurchaseItemId { get; private set; }
    public long? OrderItemId { get; private set; }
    public long? EmployeeId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal? UnitCost { get; private set; }
    public decimal? TotalCost { get; private set; }
    public string? DocumentNumber { get; private set; }
    public DateTime MovedAt { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private StockMovement() : base(0) { }

    private StockMovement(long stockItemId, long stockMovementTypeId, long? purchaseItemId, long? orderItemId, long? employeeId, decimal quantity, decimal? unitCost, decimal? totalCost, string? documentNumber, DateTime movedAt, string? notes) : base(0)
    {
        StockItemId = stockItemId;
        StockMovementTypeId = stockMovementTypeId;
        PurchaseItemId = purchaseItemId;
        OrderItemId = orderItemId;
        EmployeeId = employeeId;
        Quantity = quantity;
        UnitCost = unitCost;
        TotalCost = totalCost;
        DocumentNumber = documentNumber;
        MovedAt = movedAt;
        Notes = notes;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<StockMovement> Create(long stockItemId, long stockMovementTypeId, long? purchaseItemId, long? orderItemId, long? employeeId, decimal quantity, decimal? unitCost, decimal? totalCost, string? documentNumber, DateTime movedAt, string? notes)
    {
        // No required-string invariants for this entity.
        return Result.Success(new StockMovement(stockItemId, stockMovementTypeId, purchaseItemId, orderItemId, employeeId, quantity, unitCost, totalCost, documentNumber, movedAt, notes));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
