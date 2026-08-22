using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Categories;

// Fase 10 — detalhe de uma categoria (GET catalog/v2.0/merchants/{merchantId}/catalogs/{catalogId}/categories/{categoryId}).
// Reusa IFoodCategoryResponse (definido em ListIFoodCategoriesQuery.cs).
public sealed record GetIFoodCategoryQuery(long BranchId, string CatalogId, string CategoryId, bool IncludeItems = false)
    : IQuery<IFoodCategoryResponse>;
