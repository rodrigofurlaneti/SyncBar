namespace SyncBar.Application.Features.Catalog.GetProductById
{
    public sealed record ProductResponse(
        long Id,
        long CategoryId,
        long UnitOfMeasureId,
        string Name,
        string? Description,
        string? Barcode,
        decimal SalePrice,
        decimal? CostPrice,
        bool IsStockControlled,
        int? PreparationTimeMinutes,
        string? ImageUrl
    )
    {
        public bool HasOptionalExtras { get; init; }
        public bool HasBoosts { get; init; }
        public IReadOnlyCollection<ProductExtras.ProductOptionalExtraResponse> OptionalExtras { get; init; } = [];
        public IReadOnlyCollection<ProductExtras.ProductBoostResponse> Boosts { get; init; } = [];
    }
}
