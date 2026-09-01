using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Categories;

// Fase 10 — lista os itens vendáveis de um grupo (GET catalog/v2.0/merchants/{merchantId}/sellable-items?groupId=...).
public sealed record IfoodSellableItemResponse(
    string? ItemId, string? CategoryId, string? ItemName, string? ItemExternalCode, string? ItemEan, decimal? ItemPriceValue);

public sealed record ListIfoodSellableItemsQuery(long BranchId, string GroupId)
    : IQuery<IReadOnlyCollection<IfoodSellableItemResponse>>;
