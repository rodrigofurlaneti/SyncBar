using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;

namespace SyncBar.Application.Features.Access.SetJobTitleFeatures;

public sealed record SetJobTitleFeaturesCommand(long JobTitleId, List<long> FeatureIds) : ICommand<Result>;