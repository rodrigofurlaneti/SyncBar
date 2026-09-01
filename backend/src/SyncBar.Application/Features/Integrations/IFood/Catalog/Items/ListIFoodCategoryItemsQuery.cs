using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Items;

// Fase 10 — lista os itens de uma categoria (GET catalog/v2.0/merchants/{merchantId}/categories/{categoryId}/items).
// Estrutura profunda exposta via RawPayload, mesmo critério do restante do módulo Catalog.
public sealed record IfoodCategoryItemsResponse(string? RawPayload);

public sealed record ListIfoodCategoryItemsQuery(long BranchId, string CategoryId) : IQuery<IfoodCategoryItemsResponse>;
