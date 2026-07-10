using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Access.GetUserFeatures;

public sealed record GetUserFeaturesQuery(long AppUserId) : IQuery<IReadOnlyCollection<long>>;
