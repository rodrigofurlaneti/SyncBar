namespace SyncBar.Application.Abstractions.Integrations.Ifood;

public sealed record IfoodCatalogActionResult(bool Success, string? ErrorMessage);
public sealed record IfoodCreateCategoryResult(bool Success, string? IfoodCategoryId, string? ErrorMessage);

// Fase 6a (extensão): uma opção dentro de um optionGroup — espelha a mesma hierarquia
// item→product já usada em IfoodUpsertItemRequest (cada "option" embrulha seu próprio
// "product", ver comentário em IfoodComplementMapping). ProductId é o
// IfoodComplementMapping.IfoodProductId; OptionId é o IfoodComplementMapping.IfoodOptionId.
// ⚠️ Nomes de campo do JSON (ver IfoodCatalogClient.UpsertItemAsync) montados por analogia com
// o restante do módulo Catalog — ainda NÃO confirmados campo-a-campo contra uma resposta real
// de sandbox (mesma ressalva já registrada nas fases 4/5 para módulos sem a doc completa colada
// no momento da implementação).
public sealed record IfoodUpsertItemOption(Guid OptionId, Guid ProductId, string Name, decimal Price, bool Available);

// Fase 6a (extensão): um grupo de opções (ex.: "Escolha uma bebida") vinculado ao item —
// GroupId é o IfoodComplementGroupMapping.IfoodOptionGroupId.
public sealed record IfoodUpsertItemOptionGroup(
    Guid GroupId, string Name, int MinOptions, int MaxOptions, IReadOnlyCollection<IfoodUpsertItemOption> Options);

// Payload pra criar/atualizar um item "DEFAULT" (opcionalmente com complementos — Fase 6a).
// Pizza e combo continuam fora do escopo (ver comentário da interface). PUT /items é
// idempotente: reenviar o mesmo ItemId/ProductId nunca cria duplicata.
public sealed record IfoodUpsertItemRequest(
    Guid ItemId,
    string IfoodCategoryId,
    bool Available,
    decimal Price,
    string ExternalCode,
    Guid ProductId,
    string ProductName,
    string? ProductDescription,
    string ProductExternalCode,
    // Fase 6a (extensão): grupos de complemento vinculados ao produto (ProductComplementGroup),
    // já resolvidos com os mapeamentos Ifood da filial — vazio quando o produto não tem nenhum.
    IReadOnlyCollection<IfoodUpsertItemOptionGroup>? OptionGroups = null);

// ============================================================================================
// Fase 10 — cobertura completa do módulo Catalog (56 endpoints v1 + 40 v2, ver
// claude/auditoria-endpoints-Ifood.md). Decisão de escopo (documentada em detalhe na doc do
// projeto): a v2 (versão viva, já usada pela sincronização automática desde a Fase 3) ganha
// implementação tipada dedicada — todos os 40 endpoints abaixo. A v1 (legada — a própria v2 tem
// endpoints de upgrade/downgrade de versão, sinalizando que o Ifood está migrando os merchants
// pra v2) é coberta via um despachante genérico (InvokeCatalogV1Async) que alcança os 56
// endpoints sem duplicar um sistema de tipos inteiro para uma API que nenhum merchant do SyncBar
// usa hoje (a sincronização automática já é 100% v2). Isso fecha 100% de alcance HTTP do módulo
// sem inflar desproporcionalmente o código de produção.
//
// Nomes de campo dos DTOs abaixo foram confirmados contra os exemplos de request/response reais
// das collections Postman oficiais (módulo Catalog v1 e v2, extraídos via jq em 2026-08-21) —
// mesmo nível de confiança dos módulos Order/Merchant/Logistics (Fase 9c). Estruturas muito
// profundas (item flat completo, lista de itens de categoria) são expostas via RawPayload em vez
// de tipadas campo-a-campo, mesmo critério já usado pro Logistics "Get Order Details" (Fase 9c).
// ============================================================================================

#region Catalogs / Categories / Sellable items

public sealed record IfoodCatalogSummaryDto(string? CatalogId, string? Status, IReadOnlyCollection<string>? Context, string? GroupId, DateTime? ModifiedAt);
public sealed record IfoodCatalogsListResult(bool Success, IReadOnlyCollection<IfoodCatalogSummaryDto> Catalogs, string? ErrorMessage);

public sealed record IfoodCategoryDto(string? Id, int? Index, string? Name, string? ExternalCode, string? Status, string? Template);
public sealed record IfoodCategoryListResult(bool Success, IReadOnlyCollection<IfoodCategoryDto> Categories, string? ErrorMessage);
public sealed record IfoodCategoryDetailResult(bool Success, IfoodCategoryDto? Category, string? RawPayload, string? ErrorMessage);

public sealed record IfoodSellableItemDto(string? ItemId, string? CategoryId, string? ItemName, string? ItemExternalCode, string? ItemEan, decimal? ItemPriceValue);
public sealed record IfoodSellableItemsResult(bool Success, IReadOnlyCollection<IfoodSellableItemDto> Items, string? ErrorMessage);

#endregion

#region Items (v2 — flat)

public sealed record IfoodItemFlatResult(bool Success, string? ItemId, string? Status, decimal? PriceValue, string? ExternalCode, string? CategoryId, string? RawPayload, string? ErrorMessage);
public sealed record IfoodCategoryItemsResult(bool Success, string? RawPayload, string? ErrorMessage);
public sealed record IfoodItemPriceByCatalog(decimal Value, string CatalogContext, decimal? OriginalValue = null);
public sealed record IfoodItemExternalCodeByCatalog(string ExternalCode, string CatalogContext);

#endregion

#region Products

public sealed record IfoodProductShift(string StartTime, string EndTime, bool Monday, bool Tuesday, bool Wednesday, bool Thursday, bool Friday, bool Saturday, bool Sunday);

public sealed record IfoodProductDto(
    string? Id, string? Name, string? Description, string? AdditionalInformation, string? ExternalCode,
    string? Ean, bool? Industrialized, string? ImagePath);

public sealed record IfoodProductListResult(bool Success, IReadOnlyCollection<IfoodProductDto> Products, string? ErrorMessage);
public sealed record IfoodProductDetailResult(bool Success, IfoodProductDto? Product, string? ErrorMessage);

public sealed record IfoodUpsertProductRequest(
    string? Id, string Name, string? Description, string? AdditionalInformation, string? ExternalCode,
    string? Ean, string? Image, IReadOnlyCollection<IfoodProductShift>? Shifts = null);

public sealed record IfoodBatchProductStatusItem(string? ProductId, string? ExternalCode, string Status, IReadOnlyCollection<string>? Resources = null);
public sealed record IfoodBatchProductPriceItem(string? ProductId, string? ExternalCode, decimal Value, decimal? OriginalValue = null, IReadOnlyCollection<string>? Resources = null);
public sealed record IfoodBatchDispatchResult(bool Success, string? Url, string? BatchId, string? ErrorMessage);

#endregion

#region Option groups / Options (v2 — manutenção; criação acontece via UpsertItemAsync)

public sealed record IfoodOptionGroupDto(string? Id, string? Name, string? ExternalCode, string? Status, int? Index);
public sealed record IfoodOptionGroupListResult(bool Success, IReadOnlyCollection<IfoodOptionGroupDto> OptionGroups, string? ErrorMessage);

#endregion

#region Inventory / Batch results

public sealed record IfoodInventoryDto(string? ProductId, string? OwnerId, int? Amount, bool? InStock);
public sealed record IfoodInventoryResult(bool Success, IfoodInventoryDto? Inventory, string? ErrorMessage);

public sealed record IfoodBatchStatusResultItem(string? ResourceId, string? Result, string? FailureReason);
public sealed record IfoodBatchStatusResult(bool Success, string? BatchStatus, IReadOnlyCollection<IfoodBatchStatusResultItem> Results, string? ErrorMessage);

#endregion

#region Version / Image

public sealed record IfoodCatalogVersionResult(bool Success, string? Version, string? ErrorMessage);

// ⚠️ RISCO CONHECIDO: a doc oficial não documenta o schema do corpo/resposta deste endpoint
// (Postman mostra literalmente "<object>" pros dois, sem exemplo de campo algum) — diferente do
// resto do módulo Catalog, aqui não há nada pra confirmar campo-a-campo. O SyncBar aceita o JSON
// pronto que o chamador mandar (ex.: um objeto com a imagem em base64, convenção comum em outras
// APIs do Ifood) e repassa cru; a resposta também é devolvida crua. Tratar como não confiável até
// testar contra o sandbox.
public sealed record IfoodImageUploadResult(bool Success, string? RawPayload, string? ErrorMessage);

#endregion

#region Catálogo v1 (legado) — despachante genérico

/// <summary>
/// Os 56 endpoints do módulo Catalog v1 (<c>catalog/v1.0</c>) que não têm implementação tipada
/// dedicada nesta fase — ver comentário da interface sobre a decisão de escopo. Cada valor mapeia
/// pra um método HTTP + template de rota fixos (ver IfoodCatalogClient.V1Operations), preenchidos
/// em runtime com routeParams/queryParams/jsonBody. Nomeação e agrupamento espelham exatamente os
/// nomes da collection Postman oficial (extraídos via jq em 2026-08-21).
/// </summary>
public enum IfoodCatalogV1Operation
{
    ListCatalogs,
    ListUnsellableItems,
    ListCategories,
    CreateCategory,
    GetCategory,
    EditCategory,
    DeleteCategory,
    ListSellableItems,
    EditAisleGroupId,
    UpdateItemStatusByItemId,
    UpdateOptionStatusByItemIdAndOptionId,
    GetItem,
    EditItemStatus,
    CreateItem,
    EditItem,
    DeleteItem,
    CreateOptionGroup,
    ListOptionGroups,
    UpdateOptionGroup,
    DeleteOptionGroup,
    AssociateOptionGroupToProduct,
    UpdateOptionGroupProductAssociation,
    DisassociateOptionGroupFromProduct,
    CreateOption,
    UpdateOption,
    DeleteOption,
    UpdateOptionGroupStatus,
    ListProducts,
    CreateProduct,
    EditProduct,
    DeleteProduct,
    UpdateProductStatus,
    BatchUpdateProductStatuses,
    BatchUpdateProductPrices,
    ListProductsByExternalCode,
    BatchUpdateStatusByExternalCode,
    GetProductById,
    CreatePizza,
    ListPizzas,
    UpdatePizza,
    UpdatePizzaStatus,
    LinkPizzaToCategory,
    UnlinkPizzaFromCategory,
    BatchUpdatePizzaPricesByExternalCode,
    BatchUpdatePizzaPrices,
    GetBatchResults,
    UpsertInventory,
    GetInventory,
    DeleteInventoryBatch,
    MultisetupUpsertItem,
    MultisetupUpdateOptionPrice,
    MultisetupUpdateOptionStatus,
    MultisetupDeleteCategory,
    MultisetupListCategoryItems,
    MultisetupDeleteOptionGroup,
    MultisetupIsMultisetup,
}

public sealed record IfoodRawApiResult(bool Success, int StatusCode, string? ResponseBody, string? ErrorMessage);

#endregion

/// <summary>
/// Cliente HTTP do módulo Catalog do Ifood.
///
/// Fase 3/6a: endpoints e formatos confirmados contra a documentação oficial (Introdução, Como
/// funciona, Fundamentos, Padrões comuns, Gerenciar complementos, Gerenciar disponibilidade).
/// Cobre o "fluxo essencial" usado pela sincronização automática (Fase 3):
/// criar categoria, criar/atualizar item simples (PUT /items, com optionGroups desde a Fase 6a),
/// pausar/reativar item (PATCH /items/status) e definir estoque (POST /inventory).
///
/// Fase 10: cobertura completa do módulo — ver comentário da região "Catálogo v1 (legado)" acima
/// e a doc do projeto (Fase 10) para a decisão de escopo v2-tipado vs v1-genérico.
///
/// ⚠️ Correção da Fase 10: CreateCategoryAsync tinha um bug desde a Fase 3 — chamava
/// merchants/{merchantId}/categories (sem catalogId), path que não existe na doc oficial (a
/// criação de categoria SEMPRE exige catalogId: merchants/{merchantId}/catalogs/{catalogId}/categories).
/// Corrigido nesta fase — ver assinatura nova de CreateCategoryAsync e IfoodCatalogResolution
/// (Features/Integrations/Ifood/Catalog/IfoodCatalogResolution.cs) que resolve o catalogId antes
/// de qualquer chamada que precise dele.
/// </summary>
public interface IIfoodCatalogClient
{
    // --- Fluxo essencial (Fase 3/6a) ---
    Task<IfoodCreateCategoryResult> CreateCategoryAsync(string accessToken, string merchantId, string catalogId, string name, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> UpsertItemAsync(string accessToken, string merchantId, IfoodUpsertItemRequest request, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> SetItemStatusAsync(string accessToken, string merchantId, Guid itemId, bool available, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> SetInventoryAsync(string accessToken, string merchantId, Guid productId, int quantity, CancellationToken cancellationToken = default);

    // --- Catalogs / Categories / Sellable items (v2) ---
    Task<IfoodCatalogsListResult> GetCatalogsAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default);
    Task<IfoodCategoryListResult> ListCategoriesAsync(string accessToken, string merchantId, string catalogId, bool includeItems = false, CancellationToken cancellationToken = default);
    Task<IfoodCategoryDetailResult> GetCategoryAsync(string accessToken, string merchantId, string catalogId, string categoryId, bool includeItems = false, CancellationToken cancellationToken = default);
    Task<IfoodCategoryDetailResult> EditCategoryAsync(string accessToken, string merchantId, string catalogId, string categoryId, string? name, string? externalCode, string? status, int? index, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> DeleteCategoryAsync(string accessToken, string merchantId, string categoryId, CancellationToken cancellationToken = default);
    Task<IfoodSellableItemsResult> ListSellableItemsAsync(string accessToken, string merchantId, string groupId, CancellationToken cancellationToken = default);

    // --- Items (v2 — flat) ---
    Task<IfoodItemFlatResult> GetItemFlatAsync(string accessToken, string merchantId, Guid itemId, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> SetItemPriceAsync(string accessToken, string merchantId, Guid itemId, decimal value, decimal? originalValue, IReadOnlyCollection<IfoodItemPriceByCatalog>? priceByCatalog = null, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> SetItemExternalCodeAsync(string accessToken, string merchantId, Guid itemId, string? externalCode, IReadOnlyCollection<IfoodItemExternalCodeByCatalog>? byCatalog = null, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> DeleteItemAsync(string accessToken, string merchantId, string categoryId, Guid productId, string? catalogContext = null, CancellationToken cancellationToken = default);
    Task<IfoodCategoryItemsResult> ListCategoryItemsAsync(string accessToken, string merchantId, string categoryId, CancellationToken cancellationToken = default);

    // --- Products (v2) ---
    Task<IfoodProductListResult> ListProductsAsync(string accessToken, string merchantId, int? limit = null, int? page = null, CancellationToken cancellationToken = default);
    Task<IfoodProductDetailResult> CreateProductAsync(string accessToken, string merchantId, IfoodUpsertProductRequest request, CancellationToken cancellationToken = default);
    Task<IfoodProductDetailResult> EditProductAsync(string accessToken, string merchantId, Guid productId, IfoodUpsertProductRequest request, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> DeleteProductAsync(string accessToken, string merchantId, Guid productId, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> BatchUpdateProductStatusesAsync(string accessToken, string merchantId, IReadOnlyCollection<IfoodBatchProductStatusItem> items, string? catalogContext = null, CancellationToken cancellationToken = default);
    Task<IfoodBatchDispatchResult> BatchUpdateProductPricesAsync(string accessToken, string merchantId, IReadOnlyCollection<IfoodBatchProductPriceItem> items, string? catalogContext = null, CancellationToken cancellationToken = default);
    Task<IfoodProductListResult> ListProductsByExternalCodeAsync(string accessToken, string merchantId, string externalCode, CancellationToken cancellationToken = default);
    Task<IfoodProductDetailResult> GetProductByIdAsync(string accessToken, string merchantId, Guid productId, CancellationToken cancellationToken = default);

    // --- Option groups / Options (v2 — manutenção) ---
    Task<IfoodOptionGroupListResult> ListOptionGroupsAsync(string accessToken, string merchantId, bool includeOptions = false, string? catalogContext = null, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> UpdateOptionGroupAsync(string accessToken, string merchantId, Guid optionGroupId, string name, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> DeleteOptionGroupAsync(string accessToken, string merchantId, Guid optionGroupId, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> DisassociateOptionGroupFromProductAsync(string accessToken, string merchantId, Guid optionGroupId, Guid productId, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> DeleteOptionAsync(string accessToken, string merchantId, Guid optionGroupId, Guid productId, string? catalogContext = null, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> UpdateOptionGroupStatusAsync(string accessToken, string merchantId, Guid optionGroupId, bool available, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> SetOptionPriceAsync(string accessToken, string merchantId, Guid optionId, decimal value, decimal? originalValue, string? parentCustomizationOptionId = null, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> SetOptionExternalCodeAsync(string accessToken, string merchantId, Guid optionId, string externalCode, string? parentCustomizationOptionId = null, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> SetOptionStatusAsync(string accessToken, string merchantId, Guid optionId, bool available, string? parentCustomizationOptionId = null, CancellationToken cancellationToken = default);

    // --- Inventory / Batch results (v2) ---
    Task<IfoodInventoryResult> GetInventoryAsync(string accessToken, string merchantId, Guid productId, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> DeleteInventoryBatchAsync(string accessToken, string merchantId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default);
    Task<IfoodBatchStatusResult> GetBatchResultAsync(string accessToken, string merchantId, string batchId, CancellationToken cancellationToken = default);

    // --- Version (v2) ---
    Task<IfoodCatalogVersionResult> CheckVersionAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> UpgradeVersionAsync(string accessToken, string merchantId, bool? cleanMigration = null, CancellationToken cancellationToken = default);
    Task<IfoodCatalogActionResult> DowngradeVersionAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default);

    // --- Image (v2) ---
    Task<IfoodImageUploadResult> UploadImageAsync(string accessToken, string merchantId, string jsonBody, CancellationToken cancellationToken = default);

    // --- Catálogo v1 (legado) — despachante genérico, ver região acima ---
    Task<IfoodRawApiResult> InvokeCatalogV1Async(
        string accessToken, string merchantId, IfoodCatalogV1Operation operation,
        IReadOnlyDictionary<string, string>? routeParams = null,
        IReadOnlyDictionary<string, string>? queryParams = null,
        string? jsonBody = null,
        CancellationToken cancellationToken = default);
}
