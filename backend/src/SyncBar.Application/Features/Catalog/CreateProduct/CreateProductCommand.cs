using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.CreateProduct;

public sealed record CreateProductCommand(
    long CompanyId,
    long CategoryId,
    long UnitOfMeasureId,
    string Name,
    string? Description,
    string? Barcode,
    decimal SalePrice,
    decimal? CostPrice,
    bool IsStockControlled,
    int? PreparationTimeMinutes) : ICommand<long>;
