using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Cash.GetSummary;

internal sealed class GetCashSummaryQueryHandler(
    ICashSessionRepository cashSessionRepository,
    ISaleRepository saleRepository,
    ICashMovementRepository cashMovementRepository,
    IOrderPartialPaymentRepository partialPaymentRepository)
    : IQueryHandler<GetCashSummaryQuery, CashSummaryResponse>
{
    public async Task<Result<CashSummaryResponse>> Handle(GetCashSummaryQuery request, CancellationToken cancellationToken)
    {
        var session = await cashSessionRepository.GetByIdAsync(request.CashSessionId, cancellationToken);
        if (session is null || !session.IsActive)
            return Result.Failure<CashSummaryResponse>(new Error("CashSession.NotFound", "Cash session not found."));

        var sales = await saleRepository.GetByCashSessionAsync(session.Id, cancellationToken);
        var movements = await cashMovementRepository.GetBySessionAsync(session.Id, cancellationToken);
        var partials = await partialPaymentRepository.GetByCashSessionAsync(session.Id, cancellationToken);

        var paymentTotals = sales
            .Where(s => s.IsActive)
            .SelectMany(s => s.Payments)
            .Where(p => p.IsActive)
            .GroupBy(p => p.PaymentMethodId)
            .OrderBy(g => g.Key)
            .Select(g => new PaymentMethodTotalResponse(g.Key, g.Sum(p => p.Amount - (p.ChangeAmount ?? 0))))
            .ToList();

        var response = new CashSummaryResponse(
            session.Id,
            session.OpeningAmount,
            sales.Count(s => s.IsActive),
            sales.Where(s => s.IsActive).Sum(s => s.TotalAmount),
            paymentTotals,
            movements.Where(m => m.CashMovementTypeId == CashMovementTypeIds.Suprimento).Sum(m => m.Amount),
            movements.Where(m => m.CashMovementTypeId == CashMovementTypeIds.Sangria).Sum(m => m.Amount),
            movements.Where(m => m.CashMovementTypeId == CashMovementTypeIds.Despesa).Sum(m => m.Amount),
            partials.Sum(p => p.Amount),
            CashMath.ExpectedCash(session.OpeningAmount, sales, movements, partials));

        return Result.Success(response);
    }
}
