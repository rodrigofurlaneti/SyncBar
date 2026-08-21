using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.Complements.GetProductComplementGroups;

public sealed record GetProductComplementGroupsQuery(long ProductId) : IQuery<IReadOnlyCollection<ProductComplementGroupResponse>>;
