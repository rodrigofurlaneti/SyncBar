using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class Purchase : AggregateRoot
{
    private readonly List<PurchaseItem> _items = [];

    public long BranchId { get; private set; }
    public long SupplierId { get; private set; }
    public string? DocumentNumber { get; private set; }
    public DateTime PurchasedAt { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<PurchaseItem> Items => _items.AsReadOnly();

    private Purchase() : base(0) { }

    private Purchase(long branchId, long supplierId, string? documentNumber, DateTime purchasedAt, string? notes) : base(0)
    {
        BranchId = branchId;
        SupplierId = supplierId;
        DocumentNumber = documentNumber;
        PurchasedAt = purchasedAt;
        Notes = notes;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    // TotalAmount agora e derivado dos itens (RecalculateTotal) — nunca informado direto,
    // evita divergencia entre o valor da nota e a soma dos itens lancados.
    public static Result<Purchase> Create(long branchId, long supplierId, string? documentNumber, DateTime purchasedAt, string? notes)
        => Result.Success(new Purchase(branchId, supplierId, documentNumber, purchasedAt, notes));

    public Result AddItem(long productId, decimal quantity, decimal unitCost)
    {
        if (quantity <= 0)
            return Result.Failure(new Error("Purchase.InvalidQuantity", "Quantity must be greater than zero."));
        if (unitCost < 0)
            return Result.Failure(new Error("Purchase.InvalidUnitCost", "Unit cost cannot be negative."));

        var totalCost = Math.Round(quantity * unitCost, 2);
        var item = PurchaseItem.Create(Id, productId, quantity, unitCost, totalCost);
        if (item.IsFailure)
            return Result.Failure(item.Error);

        _items.Add(item.Value);
        RecalculateTotal();
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    private void RecalculateTotal()
        => TotalAmount = _items.Where(i => i.IsActive).Sum(i => i.TotalCost);

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
