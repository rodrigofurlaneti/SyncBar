namespace SyncBar.Application.Features.Catalog;

/// <summary>
/// DTO da tela de gerenciamento de cardápio (admin) — ao contrário de CategoryResponse
/// (usado por telas de pedido/venda), inclui categorias desativadas e expõe IsActive,
/// para o front poder mostrar o toggle e o filtro Ativos/Inativos.
/// </summary>
public sealed record CategoryManagementResponse(
    long Id,
    string Name,
    int DisplayOrder,
    bool IsActive,
    int ProductCount);
