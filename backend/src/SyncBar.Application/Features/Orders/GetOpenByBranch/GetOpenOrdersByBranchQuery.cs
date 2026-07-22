using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.GetOpenByBranch;

public sealed record GetOpenOrdersByBranchQuery(long BranchId) : IQuery<IReadOnlyCollection<OrderResponse>>;
