using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Access.GetFeatures;

public sealed record GetFeaturesQuery : IQuery<IReadOnlyCollection<FeatureResponse>>;
