using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Products;

// Fase 10 — exclui um produto (DELETE catalog/v2.0/merchants/{merchantId}/products/{productId}).
public sealed record DeleteIFoodProductCommand(long BranchId, Guid ProductId) : ICommand;
