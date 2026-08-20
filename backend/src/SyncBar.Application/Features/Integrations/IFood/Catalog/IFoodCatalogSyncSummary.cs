namespace SyncBar.Application.Features.Integrations.IFood.Catalog;

public sealed record IFoodCatalogSyncSummary(
    bool Skipped,
    int BranchesSynced,
    int CategoriesCreated,
    int ProductsSynced,
    int ProductsPaused,
    int Errors);
