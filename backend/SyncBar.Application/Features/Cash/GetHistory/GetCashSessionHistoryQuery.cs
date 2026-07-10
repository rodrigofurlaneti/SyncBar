using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Cash.GetHistory;

public sealed record CashSessionHistoryResponse(
    long Id,
    string CashRegisterName,
    long CashSessionStatusId,
    string? OpenedByName,
    string? ClosedByName,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    decimal OpeningAmount,
    decimal? ExpectedAmount,
    decimal? ClosingAmount,
    decimal? DifferenceAmount,
    decimal SalesTotal,
    int SalesCount);

public sealed record GetCashSessionHistoryQuery(
    long BranchId,
    int ReferenceYear,
    int ReferenceMonth) : IQuery<IReadOnlyCollection<CashSessionHistoryResponse>>;
