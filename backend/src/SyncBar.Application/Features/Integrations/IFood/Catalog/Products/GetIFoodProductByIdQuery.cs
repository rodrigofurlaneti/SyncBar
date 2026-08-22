using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Products;

// Fase 10 — busca um produto por Id (GET catalog/v2.0/merchants/{merchantId}/products/{productId}).
public sealed record GetIFoodProductByIdQuery(long BranchId, Guid ProductId) : IQuery<IFoodProductResponse>;
