using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Categories;

// Fase 10 — lista as categorias de um catálogo (GET catalog/v2.0/merchants/{merchantId}/catalogs/{catalogId}/categories).
// IfoodCategoryResponse é compartilhado por todos os handlers do módulo Categories que
// devolvem/recebem uma categoria (Get/Create/Edit).
public sealed record IfoodCategoryResponse(
    string? Id, int? Index, string? Name, string? ExternalCode, string? Status, string? Template);

public sealed record ListIfoodCategoriesQuery(long BranchId, string CatalogId, bool IncludeItems = false)
    : IQuery<IReadOnlyCollection<IfoodCategoryResponse>>;
