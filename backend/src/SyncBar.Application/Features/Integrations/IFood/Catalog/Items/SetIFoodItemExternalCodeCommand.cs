using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Items;

// Fase 10 — atualiza o código externo de um item (PUT catalog/v2.0/merchants/{merchantId}/items/{itemId}/externalCode).
// Espelha 1:1 o IFoodItemExternalCodeByCatalog do client.
public sealed record IFoodItemExternalCodeByCatalogInput(string ExternalCode, string CatalogContext);

public sealed record SetIFoodItemExternalCodeCommand(
    long BranchId, Guid ItemId, string? ExternalCode, IReadOnlyCollection<IFoodItemExternalCodeByCatalogInput>? ByCatalog)
    : ICommand;
