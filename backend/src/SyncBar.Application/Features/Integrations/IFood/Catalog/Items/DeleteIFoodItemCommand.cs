using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Items;

// Fase 10 — exclui um item (DELETE catalog/v2.0/merchants/{merchantId}/categories/{categoryId}/items/{productId}).
public sealed record DeleteIfoodItemCommand(long BranchId, string CategoryId, Guid ProductId, string? CatalogContext) : ICommand;
