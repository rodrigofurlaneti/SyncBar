using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Items;

// Fase 10 — atualiza o preço de um item (PUT catalog/v2.0/merchants/{merchantId}/items/{itemId}/price).
// Espelha 1:1 o IfoodItemPriceByCatalog do client.
public sealed record IfoodItemPriceByCatalogInput(decimal Value, string CatalogContext, decimal? OriginalValue);

public sealed record SetIfoodItemPriceCommand(
    long BranchId, Guid ItemId, decimal Value, decimal? OriginalValue, IReadOnlyCollection<IfoodItemPriceByCatalogInput>? PriceByCatalog)
    : ICommand;
