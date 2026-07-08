using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class PurchaseItem : Entity
{
    public long PurchaseId { get; private set; }
    public long ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal TotalCost { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private PurchaseItem() : base(0) { }

    private PurchaseItem(long purchaseId, long productId, decimal quantity, decimal unitCost, decimal totalCost) : base(0)
    {
        PurchaseId = purchaseId;
        ProductId = productId;
        Quantity = quantity;
        UnitCost = unitCost;
        TotalCost = totalCost;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<PurchaseItem> Create(long purchaseId, long productId, decimal quantity, decimal unitCost, decimal totalCost)
    {
        // No required-string invariants for this entity.
        return Result.Success(new PurchaseItem(purchaseId, productId, quantity, unitCost, totalCost));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
