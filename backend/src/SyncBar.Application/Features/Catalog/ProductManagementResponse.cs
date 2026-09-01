namespace SyncBar.Application.Features.Catalog;

/// <summary>
/// DTO da tela de gerenciamento de cardápio (admin) — ao contrário de MenuItemResponse
/// (usado por telas de pedido/venda, incluindo o Cardápio Digital do cliente), inclui
/// produtos desativados e expõe IsActive, para o front poder mostrar o toggle e o
/// filtro Ativos/Inativos sem arriscar vazar itens inativos para quem está pedindo.
/// </summary>
public sealed record ProductManagementResponse(
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
    bool IsActive,
    string? ImageUrl);
