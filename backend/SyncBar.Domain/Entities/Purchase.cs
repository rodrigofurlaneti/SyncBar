using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class Purchase : AggregateRoot
{
    public long BranchId { get; private set; }
    public long SupplierId { get; private set; }
    public string? DocumentNumber { get; private set; }
    public DateTime PurchasedAt { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Purchase() : base(0) { }

    private Purchase(long branchId, long supplierId, string? documentNumber, DateTime purchasedAt, decimal totalAmount, string? notes) : base(0)
    {
        BranchId = branchId;
        SupplierId = supplierId;
        DocumentNumber = documentNumber;
        PurchasedAt = purchasedAt;
        TotalAmount = totalAmount;
        Notes = notes;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<Purchase> Create(long branchId, long supplierId, string? documentNumber, DateTime purchasedAt, decimal totalAmount, string? notes)
    {
        // No required-string invariants for this entity.
        return Result.Success(new Purchase(branchId, supplierId, documentNumber, purchasedAt, totalAmount, notes));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
