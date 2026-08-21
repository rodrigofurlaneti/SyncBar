using SyncBar.Application.Features.Catalog.Complements;

namespace SyncBar.Application.Features.Catalog;

public sealed record MenuItemResponse(
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
    string? ImageUrl,
    // Fase 6a (extensão): grupos de complemento vinculados a este produto — vazio quando o
    // produto não tem nenhum vínculo. Mesmo formato usado pela tela de gestão de Complementos.
    IReadOnlyCollection<ComplementGroupResponse> ComplementGroups);
