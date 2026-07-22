using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Cash.GetHistory;

internal sealed class GetCashSessionHistoryQueryHandler(
    ICashSessionRepository cashSessionRepository,
    ICashRegisterRepository cashRegisterRepository,
    IEmployeeRepository employeeRepository,
    ISaleRepository saleRepository)
    : IQueryHandler<GetCashSessionHistoryQuery, IReadOnlyCollection<CashSessionHistoryResponse>>
{
    public async Task<Result<IReadOnlyCollection<CashSessionHistoryResponse>>> Handle(
        GetCashSessionHistoryQuery request, CancellationToken cancellationToken)
    {
        if (request.ReferenceMonth is < 1 or > 12)
            return Result.Failure<IReadOnlyCollection<CashSessionHistoryResponse>>(
                new Error("CashHistory.InvalidMonth", "Reference month must be between 1 and 12."));

        var from = new DateTime(request.ReferenceYear, request.ReferenceMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1);

        var sessions = await cashSessionRepository.GetByBranchAndPeriodAsync(request.BranchId, from, to, cancellationToken);
        var registers = await cashRegisterRepository.GetByBranchAsync(request.BranchId, cancellationToken);
        var employees = await employeeRepository.GetByBranchAsync(request.BranchId, cancellationToken);

        var history = new List<CashSessionHistoryResponse>();
        foreach (var session in sessions.OrderByDescending(s => s.OpenedAt))
        {
            var sales = await saleRepository.GetByCashSessionAsync(session.Id, cancellationToken);
            history.Add(new CashSessionHistoryResponse(
                session.Id,
                registers.FirstOrDefault(r => r.Id == session.CashRegisterId)?.Name ?? $"Caixa {session.CashRegisterId}",
                session.CashSessionStatusId,
                employees.FirstOrDefault(e => e.Id == session.OpenedByEmployeeId)?.Name,
                session.ClosedByEmployeeId.HasValue
                    ? employees.FirstOrDefault(e => e.Id == session.ClosedByEmployeeId.Value)?.Name
                    : null,
                session.OpenedAt,
                session.ClosedAt,
                session.OpeningAmount,
                session.ExpectedAmount,
                session.ClosingAmount,
                session.DifferenceAmount,
                sales.Where(s => s.IsActive).Sum(s => s.TotalAmount),
                sales.Count(s => s.IsActive)));
        }

        return Result.Success<IReadOnlyCollection<CashSessionHistoryResponse>>(history);
    }
}
