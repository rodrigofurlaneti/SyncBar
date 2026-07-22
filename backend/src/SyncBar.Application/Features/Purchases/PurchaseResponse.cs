namespace SyncBar.Application.Features.Purchases;

public sealed record PurchaseItemResponse(long ProductId, decimal Quantity, decimal UnitCost, decimal TotalCost);

public sealed record PurchaseResponse(
    long Id,
    long SupplierId,
    string? DocumentNumber,
    DateTime PurchasedAt,
    decimal TotalAmount,
    string? Notes,
    IReadOnlyCollection<PurchaseItemResponse> Items);
