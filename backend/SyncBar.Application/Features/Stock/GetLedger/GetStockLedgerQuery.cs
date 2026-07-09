using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Stock.GetLedger;

public sealed record GetStockLedgerQuery(long StockItemId) : IQuery<IReadOnlyCollection<StockMovementResponse>>;
