using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Categories;

// Fase 10 — edita uma categoria (PUT catalog/v2.0/merchants/{merchantId}/catalogs/{catalogId}/categories/{categoryId}).
// Reusa IfoodCategoryResponse (definido em ListIfoodCategoriesQuery.cs).
public sealed record EditIfoodCategoryCommand(
    long BranchId, string CatalogId, string CategoryId, string? Name, string? ExternalCode, string? Status, int? Index)
    : ICommand<IfoodCategoryResponse>;
