namespace SyncBar.Application.Features.Integrations.Ifood.Catalog;

public sealed record IfoodCatalogSyncSummary(
    bool Skipped,
    int BranchesSynced,
    int CategoriesCreated,
    int ProductsSynced,
    int ProductsPaused,
    int Errors);
