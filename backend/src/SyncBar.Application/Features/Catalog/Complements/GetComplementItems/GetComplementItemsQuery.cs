using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Catalog.Complements.GetComplementItems;

public sealed record GetComplementItemsQuery(long CompanyId) : IQuery<IReadOnlyCollection<ComplementItemResponse>>;
