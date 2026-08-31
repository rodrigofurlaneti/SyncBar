using SyncBar.Application.Features.Catalog.Complements;

namespace SyncBar.Application.Features.Catalog;

public sealed record MenuItemResponse(
    long Id,
    long CategoryId,
    string CategoryName, 
    long UnitOfMeasureId,
    string Name,
    string? Description,
    string? Barcode,
    decimal SalePrice,
    decimal? CostPrice,
    bool IsStockControlled,
    int? PreparationTimeMinutes,
    string? ImageUrl,
    IReadOnlyCollection<ComplementGroupResponse> ComplementGroups);
