using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Access.SetJobTitleFeatures;

public sealed record SetJobTitleFeaturesCommand(
    long JobTitleId,
    IReadOnlyCollection<long> FeatureIds) : ICommand;
