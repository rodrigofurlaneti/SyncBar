using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.GetCategoriesForManagement;

public sealed record GetCategoriesForManagementQuery(long CompanyId) : IQuery<IReadOnlyCollection<CategoryManagementResponse>>;
