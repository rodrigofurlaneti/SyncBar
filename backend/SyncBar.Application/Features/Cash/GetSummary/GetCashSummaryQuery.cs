using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Cash.GetSummary;

public sealed record GetCashSummaryQuery(long CashSessionId) : IQuery<CashSummaryResponse>;
