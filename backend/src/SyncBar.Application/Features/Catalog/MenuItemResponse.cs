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
    IReadOnlyCollection<ComplementGroupResponse> ComplementGroups)
{
    public bool HasOptionalExtras { get; init; }
    public bool HasBoosts { get; init; }
    public IReadOnlyCollection<MenuOptionalExtraResponse> OptionalExtras { get; init; } = [];
    public IReadOnlyCollection<MenuBoostResponse> Boosts { get; init; } = [];
}

public sealed record MenuOptionalExtraResponse(long Id, string OptionalExtraName, int DisplayOrder);
public sealed record MenuBoostResponse(long Id, string BoostName, decimal IncrementalValue, int DisplayOrder);
