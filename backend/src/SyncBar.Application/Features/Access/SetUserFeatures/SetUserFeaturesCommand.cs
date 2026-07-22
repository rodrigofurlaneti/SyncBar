using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Access.SetUserFeatures;

public sealed record SetUserFeaturesCommand(
    long AppUserId,
    IReadOnlyCollection<long> FeatureIds) : ICommand;
