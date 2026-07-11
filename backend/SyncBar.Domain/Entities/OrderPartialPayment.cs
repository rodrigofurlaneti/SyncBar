using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class OrderPartialPayment : Entity
{
    public long CustomerOrderId { get; private set; }
    public long CashSessionId { get; private set; }
    public long PaymentMethodId { get; private set; }
    public long EmployeeId { get; private set; }
    public decimal Amount { get; private set; }
    public string? AuthorizationCode { get; private set; }
    public string? PayerName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private OrderPartialPayment() : base(0) { }

    private OrderPartialPayment(long customerOrderId, long cashSessionId, long paymentMethodId,
        long employeeId, decimal amount, string? authorizationCode, string? payerName) : base(0)
    {
        CustomerOrderId = customerOrderId;
        CashSessionId = cashSessionId;
        PaymentMethodId = paymentMethodId;
        EmployeeId = employeeId;
        Amount = amount;
        AuthorizationCode = authorizationCode;
        PayerName = payerName;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<OrderPartialPayment> Create(long customerOrderId, long cashSessionId,
        long paymentMethodId, long employeeId, decimal amount, string? authorizationCode, string? payerName)
    {
        if (amount <= 0)
            return Result.Failure<OrderPartialPayment>(new Error("PartialPayment.InvalidAmount", "Amount must be greater than zero."));

        return Result.Success(new OrderPartialPayment(
            customerOrderId, cashSessionId, paymentMethodId, employeeId, amount, authorizationCode, payerName));
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
