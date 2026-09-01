using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Categories;

// Fase 10 — lista os catálogos do merchant (GET catalog/v2.0/merchants/{merchantId}/catalogs).
// Por BranchId, igual ao restante do módulo Catalog — resolve o MerchantId da filial via
// IfoodMerchantResolution.
public sealed record IfoodCatalogSummaryResponse(
    string? CatalogId, string? Status, IReadOnlyCollection<string>? Context, string? GroupId, DateTime? ModifiedAt);

public sealed record GetIfoodCatalogsQuery(long BranchId) : IQuery<IReadOnlyCollection<IfoodCatalogSummaryResponse>>;
