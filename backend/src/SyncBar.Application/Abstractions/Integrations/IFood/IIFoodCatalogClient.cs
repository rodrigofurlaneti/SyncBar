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

// ============================================================================================
// Fase 10 — cobertura completa do módulo Catalog (56 endpoints v1 + 40 v2, ver
// claude/auditoria-endpoints-ifood.md). Decisão de escopo (documentada em detalhe na doc do
// projeto): a v2 (versão viva, já usada pela sincronização automática desde a Fase 3) ganha
// implementação tipada dedicada — todos os 40 endpoints abaixo. A v1 (legada — a própria v2 tem
// endpoints de upgrade/downgrade de versão, sinalizando que o iFood está migrando os merchants
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

public sealed record IFoodCatalogSummaryDto(string? CatalogId, string? Status, IReadOnlyCollection<string>? Context, string? GroupId, DateTime? ModifiedAt);
public sealed record IFoodCatalogsListResult(bool Success, IReadOnlyCollection<IFoodCatalogSummaryDto> Catalogs, string? ErrorMessage);

public sealed record IFoodCategoryDto(string? Id, int? Index, string? Name, string? ExternalCode, string? Status, string? Template);
public sealed record IFoodCategoryListResult(bool Success, IReadOnlyCollection<IFoodCategoryDto> Categories, string? ErrorMessage);
public sealed record IFoodCategoryDetailResult(bool Success, IFoodCategoryDto? Category, string? RawPayload, string? ErrorMessage);

public sealed record IFoodSellableItemDto(string? ItemId, string? CategoryId, string? ItemName, string? ItemExternalCode, string? ItemEan, decimal? ItemPriceValue);
public sealed record IFoodSellableItemsResult(bool Success, IReadOnlyCollection<IFoodSellableItemDto> Items, string? ErrorMessage);

#endregion

#region Items (v2 — flat)

public sealed record IFoodItemFlatResult(bool Success, string? ItemId, string? Status, decimal? PriceValue, string? ExternalCode, string? CategoryId, string? RawPayload, string? ErrorMessage);
public sealed record IFoodCategoryItemsResult(bool Success, string? RawPayload, string? ErrorMessage);
public sealed record IFoodItemPriceByCatalog(decimal Value, string CatalogContext, decimal? OriginalValue = null);
public sealed record IFoodItemExternalCodeByCatalog(string ExternalCode, string CatalogContext);

#endregion

#region Products

public sealed record IFoodProductShift(string StartTime, string EndTime, bool Monday, bool Tuesday, bool Wednesday, bool Thursday, bool Friday, bool Saturday, bool Sunday);

public sealed record IFoodProductDto(
    string? Id, string? Name, string? Description, string? AdditionalInformation, string? ExternalCode,
    string? Ean, bool? Industrialized, string? ImagePath);

public sealed record IFoodProductListResult(bool Success, IReadOnlyCollection<IFoodProductDto> Products, string? ErrorMessage);
public sealed record IFoodProductDetailResult(bool Success, IFoodProductDto? Product, string? ErrorMessage);

public sealed record IFoodUpsertProductRequest(
    string? Id, string Name, string? Description, string? AdditionalInformation, string? ExternalCode,
    string? Ean, string? Image, IReadOnlyCollection<IFoodProductShift>? Shifts = null);

public sealed record IFoodBatchProductStatusItem(string? ProductId, string? ExternalCode, string Status, IReadOnlyCollection<string>? Resources = null);
public sealed record IFoodBatchProductPriceItem(string? ProductId, string? ExternalCode, decimal Value, decimal? OriginalValue = null, IReadOnlyCollection<string>? Resources = null);
public sealed record IFoodBatchDispatchResult(bool Success, string? Url, string? BatchId, string? ErrorMessage);

#endregion

#region Option groups / Options (v2 — manutenção; criação acontece via UpsertItemAsync)

public sealed record IFoodOptionGroupDto(string? Id, string? Name, string? ExternalCode, string? Status, int? Index);
public sealed record IFoodOptionGroupListResult(bool Success, IReadOnlyCollection<IFoodOptionGroupDto> OptionGroups, string? ErrorMessage);

#endregion

#region Inventory / Batch results

public sealed record IFoodInventoryDto(string? ProductId, string? OwnerId, int? Amount, bool? InStock);
public sealed record IFoodInventoryResult(bool Success, IFoodInventoryDto? Inventory, string? ErrorMessage);

public sealed record IFoodBatchStatusResultItem(string? ResourceId, string? Result, string? FailureReason);
public sealed record IFoodBatchStatusResult(bool Success, string? BatchStatus, IReadOnlyCollection<IFoodBatchStatusResultItem> Results, string? ErrorMessage);

#endregion

#region Version / Image

public sealed record IFoodCatalogVersionResult(bool Success, string? Version, string? ErrorMessage);

// ⚠️ RISCO CONHECIDO: a doc oficial não documenta o schema do corpo/resposta deste endpoint
// (Postman mostra literalmente "<object>" pros dois, sem exemplo de campo algum) — diferente do
// resto do módulo Catalog, aqui não há nada pra confirmar campo-a-campo. O SyncBar aceita o JSON
// pronto que o chamador mandar (ex.: um objeto com a imagem em base64, convenção comum em outras
// APIs do iFood) e repassa cru; a resposta também é devolvida crua. Tratar como não confiável até
// testar contra o sandbox.
public sealed record IFoodImageUploadResult(bool Success, string? RawPayload, string? ErrorMessage);

#endregion

#region Catálogo v1 (legado) — despachante genérico

/// <summary>
/// Os 56 endpoints do módulo Catalog v1 (<c>catalog/v1.0</c>) que não têm implementação tipada
/// dedicada nesta fase — ver comentário da interface sobre a decisão de escopo. Cada valor mapeia
/// pra um método HTTP + template de rota fixos (ver IFoodCatalogClient.V1Operations), preenchidos
/// em runtime com routeParams/queryParams/jsonBody. Nomeação e agrupamento espelham exatamente os
/// nomes da collection Postman oficial (extraídos via jq em 2026-08-21).
/// </summary>
public enum IFoodCatalogV1Operation
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

public sealed record IFoodRawApiResult(bool Success, int StatusCode, string? ResponseBody, string? ErrorMessage);

#endregion

/// <summary>
/// Cliente HTTP do módulo Catalog do iFood.
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
/// Corrigido nesta fase — ver assinatura nova de CreateCategoryAsync e IFoodCatalogResolution
/// (Features/Integrations/IFood/Catalog/IFoodCatalogResolution.cs) que resolve o catalogId antes
/// de qualquer chamada que precise dele.
/// </summary>
public interface IIFoodCatalogClient
{
    // --- Fluxo essencial (Fase 3/6a) ---
    Task<IFoodCreateCategoryResult> CreateCategoryAsync(string accessToken, string merchantId, string catalogId, string name, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> UpsertItemAsync(string accessToken, string merchantId, IFoodUpsertItemRequest request, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> SetItemStatusAsync(string accessToken, string merchantId, Guid itemId, bool available, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> SetInventoryAsync(string accessToken, string merchantId, Guid productId, int quantity, CancellationToken cancellationToken = default);

    // --- Catalogs / Categories / Sellable items (v2) ---
    Task<IFoodCatalogsListResult> GetCatalogsAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default);
    Task<IFoodCategoryListResult> ListCategoriesAsync(string accessToken, string merchantId, string catalogId, bool includeItems = false, CancellationToken cancellationToken = default);
    Task<IFoodCategoryDetailResult> GetCategoryAsync(string accessToken, string merchantId, string catalogId, string categoryId, bool includeItems = false, CancellationToken cancellationToken = default);
    Task<IFoodCategoryDetailResult> EditCategoryAsync(string accessToken, string merchantId, string catalogId, string categoryId, string? name, string? externalCode, string? status, int? index, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> DeleteCategoryAsync(string accessToken, string merchantId, string categoryId, CancellationToken cancellationToken = default);
    Task<IFoodSellableItemsResult> ListSellableItemsAsync(string accessToken, string merchantId, string groupId, CancellationToken cancellationToken = default);

    // --- Items (v2 — flat) ---
    Task<IFoodItemFlatResult> GetItemFlatAsync(string accessToken, string merchantId, Guid itemId, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> SetItemPriceAsync(string accessToken, string merchantId, Guid itemId, decimal value, decimal? originalValue, IReadOnlyCollection<IFoodItemPriceByCatalog>? priceByCatalog = null, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> SetItemExternalCodeAsync(string accessToken, string merchantId, Guid itemId, string? externalCode, IReadOnlyCollection<IFoodItemExternalCodeByCatalog>? byCatalog = null, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> DeleteItemAsync(string accessToken, string merchantId, string categoryId, Guid productId, string? catalogContext = null, CancellationToken cancellationToken = default);
    Task<IFoodCategoryItemsResult> ListCategoryItemsAsync(string accessToken, string merchantId, string categoryId, CancellationToken cancellationToken = default);

    // --- Products (v2) ---
    Task<IFoodProductListResult> ListProductsAsync(string accessToken, string merchantId, int? limit = null, int? page = null, CancellationToken cancellationToken = default);
    Task<IFoodProductDetailResult> CreateProductAsync(string accessToken, string merchantId, IFoodUpsertProductRequest request, CancellationToken cancellationToken = default);
    Task<IFoodProductDetailResult> EditProductAsync(string accessToken, string merchantId, Guid productId, IFoodUpsertProductRequest request, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> DeleteProductAsync(string accessToken, string merchantId, Guid productId, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> BatchUpdateProductStatusesAsync(string accessToken, string merchantId, IReadOnlyCollection<IFoodBatchProductStatusItem> items, string? catalogContext = null, CancellationToken cancellationToken = default);
    Task<IFoodBatchDispatchResult> BatchUpdateProductPricesAsync(string accessToken, string merchantId, IReadOnlyCollection<IFoodBatchProductPriceItem> items, string? catalogContext = null, CancellationToken cancellationToken = default);
    Task<IFoodProductListResult> ListProductsByExternalCodeAsync(string accessToken, string merchantId, string externalCode, CancellationToken cancellationToken = default);
    Task<IFoodProductDetailResult> GetProductByIdAsync(string accessToken, string merchantId, Guid productId, CancellationToken cancellationToken = default);

    // --- Option groups / Options (v2 — manutenção) ---
    Task<IFoodOptionGroupListResult> ListOptionGroupsAsync(string accessToken, string merchantId, bool includeOptions = false, string? catalogContext = null, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> UpdateOptionGroupAsync(string accessToken, string merchantId, Guid optionGroupId, string name, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> DeleteOptionGroupAsync(string accessToken, string merchantId, Guid optionGroupId, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> DisassociateOptionGroupFromProductAsync(string accessToken, string merchantId, Guid optionGroupId, Guid productId, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> DeleteOptionAsync(string accessToken, string merchantId, Guid optionGroupId, Guid productId, string? catalogContext = null, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> UpdateOptionGroupStatusAsync(string accessToken, string merchantId, Guid optionGroupId, bool available, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> SetOptionPriceAsync(string accessToken, string merchantId, Guid optionId, decimal value, decimal? originalValue, string? parentCustomizationOptionId = null, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> SetOptionExternalCodeAsync(string accessToken, string merchantId, Guid optionId, string externalCode, string? parentCustomizationOptionId = null, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> SetOptionStatusAsync(string accessToken, string merchantId, Guid optionId, bool available, string? parentCustomizationOptionId = null, CancellationToken cancellationToken = default);

    // --- Inventory / Batch results (v2) ---
    Task<IFoodInventoryResult> GetInventoryAsync(string accessToken, string merchantId, Guid productId, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> DeleteInventoryBatchAsync(string accessToken, string merchantId, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default);
    Task<IFoodBatchStatusResult> GetBatchResultAsync(string accessToken, string merchantId, string batchId, CancellationToken cancellationToken = default);

    // --- Version (v2) ---
    Task<IFoodCatalogVersionResult> CheckVersionAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> UpgradeVersionAsync(string accessToken, string merchantId, bool? cleanMigration = null, CancellationToken cancellationToken = default);
    Task<IFoodCatalogActionResult> DowngradeVersionAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default);

    // --- Image (v2) ---
    Task<IFoodImageUploadResult> UploadImageAsync(string accessToken, string merchantId, string jsonBody, CancellationToken cancellationToken = default);

    // --- Catálogo v1 (legado) — despachante genérico, ver região acima ---
    Task<IFoodRawApiResult> InvokeCatalogV1Async(
        string accessToken, string merchantId, IFoodCatalogV1Operation operation,
        IReadOnlyDictionary<string, string>? routeParams = null,
        IReadOnlyDictionary<string, string>? queryParams = null,
        string? jsonBody = null,
        CancellationToken cancellationToken = default);
}
