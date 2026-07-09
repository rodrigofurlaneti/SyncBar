using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;

namespace SyncBar.Application.Features.Cash;

// Apuracao do dinheiro esperado na gaveta:
// fundo de troco + suprimentos − sangrias − despesas + recebimentos em dinheiro (líquidos de troco).
internal static class CashMath
{
    internal static decimal ExpectedCash(
        decimal openingAmount,
        IReadOnlyCollection<Sale> sales,
        IReadOnlyCollection<CashMovement> movements)
    {
        var cashReceived = sales
            .Where(s => s.IsActive)
            .SelectMany(s => s.Payments)
            .Where(p => p.IsActive && p.PaymentMethodId == PaymentMethodIds.Dinheiro)
            .Sum(p => p.Amount - (p.ChangeAmount ?? 0));

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
