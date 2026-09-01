using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;

// Fase 10 — lista os grupos de opções do merchant (GET catalog/v2.0/merchants/{merchantId}/optionGroups).
public sealed record IfoodOptionGroupResponse(string? Id, string? Name, string? ExternalCode, string? Status, int? Index);

public sealed record ListIfoodOptionGroupsQuery(long BranchId, bool IncludeOptions = false, string? CatalogContext = null)
    : IQuery<IReadOnlyCollection<IfoodOptionGroupResponse>>;
