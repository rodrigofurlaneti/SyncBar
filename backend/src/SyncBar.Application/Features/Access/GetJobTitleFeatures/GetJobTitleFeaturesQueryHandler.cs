using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Access.GetJobTitleFeatures;

internal sealed class GetJobTitleFeaturesQueryHandler(IJobTitleFeatureRepository jobTitleFeatureRepository)
    : IQueryHandler<GetJobTitleFeaturesQuery, IReadOnlyCollection<long>>
{
    public async Task<Result<IReadOnlyCollection<long>>> Handle(
        GetJobTitleFeaturesQuery request, CancellationToken cancellationToken)
    {
        var links = await jobTitleFeatureRepository.GetByJobTitleAsync(request.JobTitleId, cancellationToken);
        IReadOnlyCollection<long> response = links.Select(l => l.AppFeatureId).ToList();
        return Result.Success(response);
    }
}
