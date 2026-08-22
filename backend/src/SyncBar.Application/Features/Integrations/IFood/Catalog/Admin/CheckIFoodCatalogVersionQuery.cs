using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Admin;

// Fase 10 — consulta a versão do catálogo do merchant (GET catalog/v2.0/merchants/{merchantId}/catalogVersion) —
// "v1" ou "v2". Ver UpgradeIFoodCatalogVersionCommand/DowngradeIFoodCatalogVersionCommand pra migrar.
public sealed record IFoodCatalogVersionResponse(string? Version);

public sealed record CheckIFoodCatalogVersionQuery(long BranchId) : IQuery<IFoodCatalogVersionResponse>;
