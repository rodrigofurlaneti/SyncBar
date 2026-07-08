using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class SalePayment : Entity
{
    public long SaleId { get; private set; }
    public long PaymentMethodId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal? ChangeAmount { get; private set; }
    public string? AuthorizationCode { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private SalePayment() : base(0) { }

    private SalePayment(long saleId, long paymentMethodId, decimal amount, decimal? changeAmount, string? authorizationCode) : base(0)
    {
        SaleId = saleId;
        PaymentMethodId = paymentMethodId;
        Amount = amount;
        ChangeAmount = changeAmount;
        AuthorizationCode = authorizationCode;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    internal static Result<SalePayment> Create(long saleId, long paymentMethodId, decimal amount, decimal? changeAmount, string? authorizationCode)
    {
        if (amount <= 0)
            return Result.Failure<SalePayment>(new Error("SalePayment.InvalidAmount", "Amount must be greater than zero."));

        return Result.Success(new SalePayment(saleId, paymentMethodId, amount, changeAmount, authorizationCode));
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
