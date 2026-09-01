using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Categories;

// Fase 10 — detalhe de uma categoria (GET catalog/v2.0/merchants/{merchantId}/catalogs/{catalogId}/categories/{categoryId}).
// Reusa IfoodCategoryResponse (definido em ListIfoodCategoriesQuery.cs).
public sealed record GetIfoodCategoryQuery(long BranchId, string CatalogId, string CategoryId, bool IncludeItems = false)
    : IQuery<IfoodCategoryResponse>;
