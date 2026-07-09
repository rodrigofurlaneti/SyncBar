using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Stock.RegisterMovement;

public sealed record RegisterStockMovementCommand(
    long BranchId,
    long ProductId,
    long StockMovementTypeId,
    long EmployeeId,
    decimal Quantity,
    decimal? UnitCost,
    string? DocumentNumber,
    string? Notes) : ICommand<long>;
