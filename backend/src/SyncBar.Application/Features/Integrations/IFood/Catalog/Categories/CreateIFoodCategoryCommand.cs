using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Categories;

// Fase 10 — cria uma categoria (POST catalog/v2.0/merchants/{merchantId}/catalogs/{catalogId}/categories).
public sealed record IFoodCategoryCreateResponse(string? IFoodCategoryId);

public sealed record CreateIFoodCategoryCommand(long BranchId, string CatalogId, string Name)
    : ICommand<IFoodCategoryCreateResponse>;
