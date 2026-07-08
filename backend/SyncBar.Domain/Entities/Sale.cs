using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class Sale : AggregateRoot
{
    private readonly List<SalePayment> _payments = [];

    public long BranchId { get; private set; }
    public long CustomerOrderId { get; private set; }
    public long CashSessionId { get; private set; }
    public long EmployeeId { get; private set; }
    public long SaleNumber { get; private set; }
    public decimal SubtotalAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal ServiceFeeAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime SoldAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<SalePayment> Payments => _payments.AsReadOnly();

    private Sale() : base(0) { }

    private Sale(long branchId, long customerOrderId, long cashSessionId, long employeeId, long saleNumber,
        decimal subtotalAmount, decimal discountAmount, decimal serviceFeeAmount) : base(0)
    {
        BranchId = branchId;
        CustomerOrderId = customerOrderId;
        CashSessionId = cashSessionId;
        EmployeeId = employeeId;
        SaleNumber = saleNumber;
        SubtotalAmount = subtotalAmount;
        DiscountAmount = discountAmount;
        ServiceFeeAmount = serviceFeeAmount;
        TotalAmount = subtotalAmount - discountAmount + serviceFeeAmount;
        SoldAt = DateTime.UtcNow;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<Sale> Create(long branchId, long customerOrderId, long cashSessionId, long employeeId,
        long saleNumber, decimal subtotalAmount, decimal discountAmount, decimal serviceFeeAmount)
    {
        if (subtotalAmount < 0 || discountAmount < 0 || serviceFeeAmount < 0)
            return Result.Failure<Sale>(new Error("Sale.InvalidAmounts", "Amounts cannot be negative."));

        return Result.Success(new Sale(branchId, customerOrderId, cashSessionId, employeeId, saleNumber,
            subtotalAmount, discountAmount, serviceFeeAmount));
    }

    public Result AddPayment(long paymentMethodId, decimal amount, decimal? changeAmount, string? authorizationCode, bool allowsChange)
    {
        if (amount <= 0)
            return Result.Failure(new Error("Sale.InvalidPaymentAmount", "Payment amount must be greater than zero."));
        if (changeAmount is > 0 && !allowsChange)
            return Result.Failure(new Error("Sale.ChangeNotAllowed", "Change is only allowed for cash payments."));

        var payment = SalePayment.Create(Id, paymentMethodId, amount, changeAmount, authorizationCode);
        if (payment.IsFailure)
            return Result.Failure(payment.Error);

        _payments.Add(payment.Value);
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result EnsureFullyPaid()
    {
        var paid = _payments.Where(p => p.IsActive).Sum(p => p.Amount - (p.ChangeAmount ?? 0));
        if (paid < TotalAmount)
            return Result.Failure(new Error("Sale.InsufficientPayment",
                $"Payments ({paid:0.00}) do not cover the sale total ({TotalAmount:0.00})."));
        return Result.Success();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
