using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.ProductExtras;

public sealed record ProductBoostResponse(long Id, long ProductId, string BoostName, decimal IncrementalValue, int DisplayOrder)
{
    public static ProductBoostResponse From(ProductBoost item) => new(item.Id, item.ProductId, item.BoostName, item.IncrementalValue, item.DisplayOrder);
}

