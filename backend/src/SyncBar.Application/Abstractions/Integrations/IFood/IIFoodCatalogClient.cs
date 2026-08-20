namespace SyncBar.Application.Abstractions.Integrations.IFood;

public sealed record IFoodCatalogActionResult(bool Success, string? ErrorMessage);
public sealed record IFoodCreateCategoryResult(bool Success, string? IFoodCategoryId, string? ErrorMessage);

// Payload pra criar/atualizar um item "DEFAULT" simples (sem complementos, pizza ou combo —
// fora do escopo do "fluxo essencial", ver ifood-integration-status no projeto claude.ai).
// PUT /items é idempotente: reenviar o mesmo ItemId/ProductId nunca cria duplicata.
public sealed record IFoodUpsertItemRequest(
    Guid ItemId,
    string IFoodCategoryId,
    bool Available,
    decimal Price,
    string ExternalCode,
    Guid ProductId,
    string ProductName,
    string? ProductDescription,
    string ProductExternalCode);

/// <summary>
/// Cliente HTTP do módulo Catalog do iFood — endpoints e formatos confirmados em 2026-08-19
/// contra a documentação oficial colada pelo usuário (Introdução, Como funciona, Fundamentos,
/// Padrões comuns, Gerenciar complementos, Gerenciar disponibilidade). Cobre o "fluxo essencial":
/// criar categoria, criar/atualizar item simples (PUT /items), pausar/reativar item
/// (PATCH /items/status) e definir estoque (POST /inventory).
///
/// NÃO implementado nesta fase (fora do escopo "essencial"): complementos (optionGroups/options),
/// pizzas, combos, múltiplos contextos/canais (contextModifiers), atualização em lote
/// (PATCH .../price ou .../status em lote com batchId).
/// </summary>
public interface IIFoodCatalogClient
{
    Task<IFoodCreateCategoryResult> CreateCategoryAsync(string accessToken, string merchantId, string name, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> UpsertItemAsync(string accessToken, string merchantId, IFoodUpsertItemRequest request, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> SetItemStatusAsync(string accessToken, string merchantId, Guid itemId, bool available, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> SetInventoryAsync(string accessToken, string merchantId, Guid productId, int quantity, CancellationToken cancellationToken = default);
}
