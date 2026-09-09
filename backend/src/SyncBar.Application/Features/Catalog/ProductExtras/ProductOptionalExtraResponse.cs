using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.ProductExtras;

public sealed record ProductOptionalExtraResponse(long Id, long ProductId, string OptionalExtraName, int DisplayOrder)
{
    public static ProductOptionalExtraResponse From(ProductOptionalExtra item) => new(item.Id, item.ProductId, item.OptionalExtraName, item.DisplayOrder);
}

