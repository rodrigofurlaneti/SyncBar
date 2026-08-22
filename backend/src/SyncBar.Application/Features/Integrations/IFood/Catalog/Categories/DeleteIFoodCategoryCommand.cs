using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Categories;

// Fase 10 — exclui uma categoria (DELETE catalog/v2.0/merchants/{merchantId}/categories/{categoryId}).
// Diferente de Create/Edit/Get, o path de exclusão da v2 não exige catalogId — apenas categoryId.
public sealed record DeleteIFoodCategoryCommand(long BranchId, string CategoryId) : ICommand;
