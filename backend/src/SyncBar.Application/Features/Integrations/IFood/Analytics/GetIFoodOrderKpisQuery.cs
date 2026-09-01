using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Analytics;

public sealed record GetIfoodOrderKpisQuery(long BranchId, DateTime? PeriodStart, DateTime? PeriodEnd, int Page)
    : IQuery<IfoodOrderKpisResponse>;
