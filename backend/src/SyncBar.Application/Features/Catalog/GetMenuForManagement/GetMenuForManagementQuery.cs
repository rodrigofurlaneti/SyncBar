using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.GetMenuForManagement;

public sealed record GetMenuForManagementQuery(long CompanyId) : IQuery<IReadOnlyCollection<ProductManagementResponse>>;
