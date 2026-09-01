using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Items;

// Fase 10 — atualiza o código externo de um item (PUT catalog/v2.0/merchants/{merchantId}/items/{itemId}/externalCode).
// Espelha 1:1 o IfoodItemExternalCodeByCatalog do client.
public sealed record IfoodItemExternalCodeByCatalogInput(string ExternalCode, string CatalogContext);

public sealed record SetIfoodItemExternalCodeCommand(
    long BranchId, Guid ItemId, string? ExternalCode, IReadOnlyCollection<IfoodItemExternalCodeByCatalogInput>? ByCatalog)
    : ICommand;
