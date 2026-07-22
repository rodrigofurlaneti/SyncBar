using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.GetMenu;

public sealed record GetMenuQuery(long CompanyId) : IQuery<IReadOnlyCollection<MenuItemResponse>>;
