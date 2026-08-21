namespace SyncBar.Application.Abstractions.Integrations.IFood;

public sealed record IFoodCatalogActionResult(bool Success, string? ErrorMessage);
public sealed record IFoodCreateCategoryResult(bool Success, string? IFoodCategoryId, string? ErrorMessage);

// Fase 6a (extensão): uma opção dentro de um optionGroup — espelha a mesma hierarquia
// item→product já usada em IFoodUpsertItemRequest (cada "option" embrulha seu próprio
// "product", ver comentário em IFoodComplementMapping). ProductId é o
// IFoodComplementMapping.IFoodProductId; OptionId é o IFoodComplementMapping.IFoodOptionId.
// ⚠️ Nomes de campo do JSON (ver IFoodCatalogClient.UpsertItemAsync) montados por analogia com
// o restante do módulo Catalog — ainda NÃO confirmados campo-a-campo contra uma resposta real
// de sandbox (mesma ressalva já registrada nas fases 4/5 para módulos sem a doc completa colada
// no momento da implementação).
public sealed record IFoodUpsertItemOption(Guid OptionId, Guid ProductId, string Name, decimal Price, bool Available);

// Fase 6a (extensão): um grupo de opções (ex.: "Escolha uma bebida") vinculado ao item —
// GroupId é o IFoodComplementGroupMapping.IFoodOptionGroupId.
public sealed record IFoodUpsertItemOptionGroup(
    Guid GroupId, string Name, int MinOptions, int MaxOptions, IReadOnlyCollection<IFoodUpsertItemOption> Options);

// Payload pra criar/atualizar um item "DEFAULT" (opcionalmente com complementos — Fase 6a).
// Pizza e combo continuam fora do escopo (ver comentário da interface). PUT /items é
// idempotente: reenviar o mesmo ItemId/ProductId nunca cria duplicata.
public sealed record IFoodUpsertItemRequest(
    Guid ItemId,
    string IFoodCategoryId,
    bool Available,
    decimal Price,
    string ExternalCode,
    Guid ProductId,
    string ProductName,
    string? ProductDescription,
    string ProductExternalCode,
    // Fase 6a (extensão): grupos de complemento vinculados ao produto (ProductComplementGroup),
    // já resolvidos com os mapeamentos iFood da filial — vazio quando o produto não tem nenhum.
    IReadOnlyCollection<IFoodUpsertItemOptionGroup>? OptionGroups = null);

/// <summary>
/// Cliente HTTP do módulo Catalog do iFood — endpoints e formatos confirmados em 2026-08-19
/// contra a documentação oficial colada pelo usuário (Introdução, Como funciona, Fundamentos,
/// Padrões comuns, Gerenciar complementos, Gerenciar disponibilidade). Cobre o "fluxo essencial":
/// criar categoria, criar/atualizar item simples (PUT /items), pausar/reativar item
/// (PATCH /items/status) e definir estoque (POST /inventory).
///
/// Fase 6a (extensão): UpsertItemAsync passou a enviar optionGroups/options reais quando o
/// produto tem ProductComplementGroup vinculado — ver comentário de IFoodUpsertItemOption
/// sobre o nível de confiança dos nomes de campo.
///
/// Ainda fora do escopo: pizzas, combos, múltiplos contextos/canais (contextModifiers),
/// atualização em lote (PATCH .../price ou .../status em lote com batchId).
/// </summary>
public interface IIFoodCatalogClient
{
    Task<IFoodCreateCategoryResult> CreateCategoryAsync(string accessToken, string merchantId, string name, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> UpsertItemAsync(string accessToken, string merchantId, IFoodUpsertItemRequest request, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> SetItemStatusAsync(string accessToken, string merchantId, Guid itemId, bool available, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> SetInventoryAsync(string accessToken, string merchantId, Guid productId, int quantity, CancellationToken cancellationToken = default);
}
