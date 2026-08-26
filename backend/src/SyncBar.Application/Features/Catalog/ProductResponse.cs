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
    );
}
