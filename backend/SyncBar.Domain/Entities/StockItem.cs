using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class StockItem : AggregateRoot
{
    public long BranchId { get; private set; }
    public long ProductId { get; private set; }
    public decimal CurrentQuantity { get; private set; }
    public decimal MinimumQuantity { get; private set; }
    public decimal? MaximumQuantity { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private StockItem() : base(0) { }

    private StockItem(long branchId, long productId, decimal minimumQuantity, decimal? maximumQuantity) : base(0)
    {
        BranchId = branchId;
        ProductId = productId;
        CurrentQuantity = 0;
        MinimumQuantity = minimumQuantity;
        MaximumQuantity = maximumQuantity;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<StockItem> Create(long branchId, long productId, decimal minimumQuantity, decimal? maximumQuantity)
    {
        if (minimumQuantity < 0)
            return Result.Failure<StockItem>(new Error("StockItem.InvalidMinimum", "Minimum quantity cannot be negative."));

        return Result.Success(new StockItem(branchId, productId, minimumQuantity, maximumQuantity));
    }

    // Saldo so muda por movimento — o chamador DEVE persistir o StockMovement correspondente.
    public Result Increase(decimal quantity)
    {
        if (quantity <= 0)
            return Result.Failure(new Error("StockItem.InvalidQuantity", "Quantity must be greater than zero."));

        CurrentQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Decrease(decimal quantity)
    {
        if (quantity <= 0)
            return Result.Failure(new Error("StockItem.InvalidQuantity", "Quantity must be greater than zero."));
        if (CurrentQuantity - quantity < 0)
            return Result.Failure(new Error("StockItem.InsufficientStock", "Stock cannot become negative."));

        CurrentQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public bool IsBelowMinimum() => CurrentQuantity < MinimumQuantity;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
