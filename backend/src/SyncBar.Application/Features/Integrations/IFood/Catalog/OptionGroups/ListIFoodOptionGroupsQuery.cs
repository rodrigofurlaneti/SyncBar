using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.OptionGroups;

// Fase 10 — lista os grupos de opções do merchant (GET catalog/v2.0/merchants/{merchantId}/optionGroups).
public sealed record IFoodOptionGroupResponse(string? Id, string? Name, string? ExternalCode, string? Status, int? Index);

public sealed record ListIFoodOptionGroupsQuery(long BranchId, bool IncludeOptions = false, string? CatalogContext = null)
    : IQuery<IReadOnlyCollection<IFoodOptionGroupResponse>>;
