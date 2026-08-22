using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Items;

// Fase 10 — item "flat" v2 (GET catalog/v2.0/merchants/{merchantId}/items/{itemId}). Estrutura
// profunda exposta via RawPayload, mesmo critério do restante do módulo Catalog (ver comentário
// da região "Items (v2 — flat)" em IIFoodCatalogClient).
public sealed record IFoodItemFlatResponse(
    string? ItemId, string? Status, decimal? PriceValue, string? ExternalCode, string? CategoryId, string? RawPayload);

public sealed record GetIFoodItemFlatQuery(long BranchId, Guid ItemId) : IQuery<IFoodItemFlatResponse>;
