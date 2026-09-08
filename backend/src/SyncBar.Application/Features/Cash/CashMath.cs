using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;

namespace SyncBar.Application.Features.Cash;

// Apuracao do dinheiro esperado na gaveta:
// fundo de troco + suprimentos − sangrias − despesas + recebimentos em dinheiro (líquidos de troco).
internal static class CashMath
{
    internal static Dictionary<long, decimal> PaymentTotals(IReadOnlyCollection<Sale> sales,
        IReadOnlyCollection<OrderPartialPayment> partialPayments) => sales
        .Where(s => s.IsActive).SelectMany(s => s.Payments).Where(p => p.IsActive)
        .Select(p => (p.PaymentMethodId, Amount: p.Amount - (p.ChangeAmount ?? 0)))
        .Concat(partialPayments.Select(p => (p.PaymentMethodId, p.Amount)))
        .GroupBy(p => p.PaymentMethodId).ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

    internal static decimal ExpectedCash(
        decimal openingAmount,
        IReadOnlyCollection<Sale> sales,
        IReadOnlyCollection<CashMovement> movements,
        IReadOnlyCollection<OrderPartialPayment>? partialPayments = null)
    {
        var cashReceived = sales
            .Where(s => s.IsActive)
            .SelectMany(s => s.Payments)
            .Where(p => p.IsActive && p.PaymentMethodId == PaymentMethodIds.Dinheiro)
            .Sum(p => p.Amount - (p.ChangeAmount ?? 0));

        // Parciais em dinheiro tambem estao na gaveta.
        cashReceived += (partialPayments ?? [])
            .Where(p => p.PaymentMethodId == PaymentMethodIds.Dinheiro)
            .Sum(p => p.Amount);

        var suprimento = movements
            .Where(m => m.CashMovementTypeId == CashMovementTypeIds.Suprimento)
            .Sum(m => m.Amount);

        var sangria = movements
            .Where(m => m.CashMovementTypeId == CashMovementTypeIds.Sangria)
            .Sum(m => m.Amount);

        var despesa = movements
            .Where(m => m.CashMovementTypeId == CashMovementTypeIds.Despesa)
            .Sum(m => m.Amount);

        return openingAmount + suprimento - sangria - despesa + cashReceived;
    }
}
