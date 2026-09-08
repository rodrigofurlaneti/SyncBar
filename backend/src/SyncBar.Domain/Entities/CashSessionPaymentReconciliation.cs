using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Conferencia por forma de pagamento no fechamento do caixa: quanto o sistema
// esperava (agregado das vendas liquidadas na sessao) vs. quanto o operador
// conferiu manualmente (Cartao de Credito/Debito/Pix), com a diferenca isolada
// por modalidade. E uma trilha de auditoria imutavel — nunca alterada apos criada,
// so desativada. O total geral de diferenca fica em CashSession.TotalDifferenceAmount.
public sealed class CashSessionPaymentReconciliation : Entity
{
    public long CashSessionId { get; private set; }
    public long PaymentMethodId { get; private set; }
    public decimal ExpectedAmount { get; private set; }
    public decimal CountedAmount { get; private set; }
    public decimal DifferenceAmount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private CashSessionPaymentReconciliation() : base(0) { }

    private CashSessionPaymentReconciliation(
        long cashSessionId, long paymentMethodId, decimal expectedAmount, decimal countedAmount) : base(0)
    {
        CashSessionId = cashSessionId;
        PaymentMethodId = paymentMethodId;
        ExpectedAmount = expectedAmount;
        CountedAmount = countedAmount;
        DifferenceAmount = countedAmount - expectedAmount;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<CashSessionPaymentReconciliation> Create(
        long cashSessionId, long paymentMethodId, decimal expectedAmount, decimal countedAmount)
    {
        if (cashSessionId <= 0)
            return Result.Failure<CashSessionPaymentReconciliation>(
                new Error("CashSessionPaymentReconciliation.InvalidCashSession", "Cash session is required."));

        if (paymentMethodId <= 0)
            return Result.Failure<CashSessionPaymentReconciliation>(
                new Error("CashSessionPaymentReconciliation.InvalidPaymentMethod", "Payment method is required."));

        if (countedAmount < 0)
            return Result.Failure<CashSessionPaymentReconciliation>(
                new Error("CashSessionPaymentReconciliation.InvalidCountedAmount", "Counted amount cannot be negative."));

        return Result.Success(new CashSessionPaymentReconciliation(cashSessionId, paymentMethodId, expectedAmount, countedAmount));
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}
