using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class ProductStock : Entity
{
    public long ProductId { get; private set; }
    public long StockItemId { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private ProductStock() : base(0) { }

    public ProductStock(long productId, long stockItemId, decimal initialBalance) : base(0)
    {
        ProductId = productId;
        StockItemId = stockItemId;
        CurrentBalance = initialBalance;
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
        return Result.Success();
    }
}