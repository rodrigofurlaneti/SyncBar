using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Stock.AdjustInventory;

public sealed record InventoryCountInput(long ProductId, decimal CountedQuantity);

public sealed record InventoryAdjustmentResponse(
    long ProductId,
    decimal PreviousQuantity,
    decimal CountedQuantity,
    decimal Difference);

public sealed record AdjustInventoryCommand(
    long BranchId,
    long EmployeeId,
    IReadOnlyCollection<InventoryCountInput> Counts)
    : ICommand<IReadOnlyCollection<InventoryAdjustmentResponse>>;
