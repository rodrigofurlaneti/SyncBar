using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Items;

// Fase 10 — atualiza o preço de um item (PUT catalog/v2.0/merchants/{merchantId}/items/{itemId}/price).
// Espelha 1:1 o IFoodItemPriceByCatalog do client.
public sealed record IFoodItemPriceByCatalogInput(decimal Value, string CatalogContext, decimal? OriginalValue);

public sealed record SetIFoodItemPriceCommand(
    long BranchId, Guid ItemId, decimal Value, decimal? OriginalValue, IReadOnlyCollection<IFoodItemPriceByCatalogInput>? PriceByCatalog)
    : ICommand;
