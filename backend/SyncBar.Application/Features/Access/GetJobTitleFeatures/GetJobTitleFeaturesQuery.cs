using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Access.GetJobTitleFeatures;

public sealed record GetJobTitleFeaturesQuery(long JobTitleId) : IQuery<IReadOnlyCollection<long>>;
