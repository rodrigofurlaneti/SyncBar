using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Products;

// Fase 10 — exclui um produto (DELETE catalog/v2.0/merchants/{merchantId}/products/{productId}).
public sealed record DeleteIfoodProductCommand(long BranchId, Guid ProductId) : ICommand;
