using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Stock.GetByBranch;

public sealed record GetStockByBranchQuery(long BranchId) : IQuery<IReadOnlyCollection<StockItemResponse>>;
