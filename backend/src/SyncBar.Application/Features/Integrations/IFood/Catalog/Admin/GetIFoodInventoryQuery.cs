using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Admin;

// Fase 10 — consulta o estoque de um produto (GET catalog/v2.0/merchants/{merchantId}/inventory/{productId}).
public sealed record IFoodInventoryResponse(string? ProductId, string? OwnerId, int? Amount, bool? InStock);

public sealed record GetIFoodInventoryQuery(long BranchId, Guid ProductId) : IQuery<IFoodInventoryResponse>;
