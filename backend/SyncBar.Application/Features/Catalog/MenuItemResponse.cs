namespace SyncBar.Application.Features.Catalog;

public sealed record MenuItemResponse(
    long Id,
    long CategoryId,
    string Name,
    string? Description,
    decimal SalePrice,
    int? PreparationTimeMinutes);
