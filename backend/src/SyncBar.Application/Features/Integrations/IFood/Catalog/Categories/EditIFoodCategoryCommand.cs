using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Categories;

// Fase 10 — edita uma categoria (PUT catalog/v2.0/merchants/{merchantId}/catalogs/{catalogId}/categories/{categoryId}).
// Reusa IFoodCategoryResponse (definido em ListIFoodCategoriesQuery.cs).
public sealed record EditIFoodCategoryCommand(
    long BranchId, string CatalogId, string CategoryId, string? Name, string? ExternalCode, string? Status, int? Index)
    : ICommand<IFoodCategoryResponse>;
