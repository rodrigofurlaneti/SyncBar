using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Categories;

// Fase 10 — lista os itens vendáveis de um grupo (GET catalog/v2.0/merchants/{merchantId}/sellable-items?groupId=...).
public sealed record IFoodSellableItemResponse(
    string? ItemId, string? CategoryId, string? ItemName, string? ItemExternalCode, string? ItemEan, decimal? ItemPriceValue);

public sealed record ListIFoodSellableItemsQuery(long BranchId, string GroupId)
    : IQuery<IReadOnlyCollection<IFoodSellableItemResponse>>;
