using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Cash.GetSummary;

internal sealed class GetCashSummaryQueryHandler(
    ICashSessionRepository cashSessionRepository,
    ISaleRepository saleRepository,
    ICashMovementRepository cashMovementRepository,
    IOrderPartialPaymentRepository partialPaymentRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetCashSummaryQuery, CashSummaryResponse>(logRepository, unitOfWork)
{
    public override Task<Result<CashSummaryResponse>> Handle(GetCashSummaryQuery request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(GetCashSummaryQueryHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se o IP estiver disponível na Query
            async (userIdBox) =>
            {
                var session = await cashSessionRepository.GetByIdAsync(request.CashSessionId, cancellationToken);
                if (session is null || !session.IsActive)
                    return Result.Failure<CashSummaryResponse>(new Error("CashSession.NotFound", "Cash session not found."));

                var sales = await saleRepository.GetByCashSessionAsync(session.Id, cancellationToken);
                var movements = await cashMovementRepository.GetBySessionAsync(session.Id, cancellationToken);
                var partials = await partialPaymentRepository.GetByCashSessionAsync(session.Id, cancellationToken);

                var paymentTotals = CashMath.PaymentTotals(sales, partials)
                    .OrderBy(g => g.Key)
                    .Select(g => new PaymentMethodTotalResponse(g.Key, g.Value))
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
                    CashMath.ExpectedCash(session.OpeningAmount, sales, movements, partials),
                    movements.Where(m => m.IsActive).OrderBy(m => m.CreatedAt)
                        .Select(m => new CashMovementResponse(m.Id, m.CashMovementTypeId, m.Amount, m.Description, m.CreatedAt)).ToList());

                return Result.Success(response);
            });
}
