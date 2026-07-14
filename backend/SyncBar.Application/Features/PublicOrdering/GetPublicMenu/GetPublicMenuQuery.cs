using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Catalog;

namespace SyncBar.Application.Features.PublicOrdering.GetPublicMenu;

public sealed record PublicMenuResponse(
    string BranchName,
    int TableNumber,
    IReadOnlyCollection<MenuItemResponse> Items);

// Sem autenticação — o "segredo" é o token do QR Code da mesa (GUID imprevisível).
public sealed record GetPublicMenuQuery(Guid Token) : IQuery<PublicMenuResponse>;
