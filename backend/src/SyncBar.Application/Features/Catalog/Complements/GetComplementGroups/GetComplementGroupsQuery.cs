using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.Complements.GetComplementGroups;

public sealed record GetComplementGroupsQuery(long CompanyId) : IQuery<IReadOnlyCollection<ComplementGroupResponse>>;
