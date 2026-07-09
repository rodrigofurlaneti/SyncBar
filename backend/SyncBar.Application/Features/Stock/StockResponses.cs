namespace SyncBar.Application.Features.Stock;

public sealed record StockItemResponse(
    long Id,
    long BranchId,
    long ProductId,
    decimal CurrentQuantity,
    decimal MinimumQuantity,
    decimal? MaximumQuantity,
    bool IsBelowMinimum);

public sealed record StockMovementResponse(
    long Id,
    long StockItemId,
    long StockMovementTypeId,
    decimal Quantity,
    decimal? UnitCost,
    decimal? TotalCost,
    string? DocumentNumber,
    DateTime MovedAt,
    string? Notes);
