using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Analytics;

public sealed record GetIFoodOrderKpisQuery(long BranchId, DateTime? PeriodStart, DateTime? PeriodEnd, int Page)
    : IQuery<IFoodOrderKpisResponse>;
