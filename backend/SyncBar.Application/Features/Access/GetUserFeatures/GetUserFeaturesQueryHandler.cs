using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Access.GetUserFeatures;

internal sealed class GetUserFeaturesQueryHandler(IAppUserFeatureRepository userFeatureRepository)
    : IQueryHandler<GetUserFeaturesQuery, IReadOnlyCollection<long>>
{
    public async Task<Result<IReadOnlyCollection<long>>> Handle(
        GetUserFeaturesQuery request, CancellationToken cancellationToken)
    {
        var links = await userFeatureRepository.GetByUserAsync(request.AppUserId, cancellationToken);
        IReadOnlyCollection<long> response = links.Select(l => l.AppFeatureId).ToList();
        return Result.Success(response);
    }
}
