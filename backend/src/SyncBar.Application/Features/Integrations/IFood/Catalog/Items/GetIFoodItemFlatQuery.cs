using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Items;

// Fase 10 — item "flat" v2 (GET catalog/v2.0/merchants/{merchantId}/items/{itemId}). Estrutura
// profunda exposta via RawPayload, mesmo critério do restante do módulo Catalog (ver comentário
// da região "Items (v2 — flat)" em IIfoodCatalogClient).
public sealed record IfoodItemFlatResponse(
    string? ItemId, string? Status, decimal? PriceValue, string? ExternalCode, string? CategoryId, string? RawPayload);

public sealed record GetIfoodItemFlatQuery(long BranchId, Guid ItemId) : IQuery<IfoodItemFlatResponse>;
