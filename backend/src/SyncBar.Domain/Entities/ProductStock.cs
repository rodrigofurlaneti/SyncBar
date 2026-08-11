using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class ProductStock
{
    public long ProductId { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public decimal MinimumQuantity { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ProductStock() { }

    public ProductStock(long productId, decimal initialBalance, decimal minimumQuantity)
    {
        ProductId = productId;
        CurrentBalance = initialBalance;
        MinimumQuantity = minimumQuantity;
        CreatedAt = DateTime.Now;
    }

    public Result Deduct(decimal quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(new Error("Stock.InvalidQuantity", "A quantidade a deduzir deve ser maior que zero."));
        }

        if (CurrentBalance < quantity)
        {
            return Result.Failure(new Error("Stock.Insufficient", $"Estoque insuficiente. Saldo atual: {CurrentBalance}, solicitado: {quantity}."));
        }

        CurrentBalance -= quantity;
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }
}