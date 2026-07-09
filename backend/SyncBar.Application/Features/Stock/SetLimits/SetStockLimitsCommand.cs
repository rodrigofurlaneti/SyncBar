using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Stock.SetLimits;

public sealed record SetStockLimitsCommand(
    long StockItemId,
    decimal MinimumQuantity,
    decimal? MaximumQuantity) : ICommand;
