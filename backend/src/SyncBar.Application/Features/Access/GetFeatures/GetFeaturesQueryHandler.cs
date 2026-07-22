using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Access.GetFeatures;

internal sealed class GetFeaturesQueryHandler(IAppFeatureRepository featureRepository)
    : IQueryHandler<GetFeaturesQuery, IReadOnlyCollection<FeatureResponse>>
{
    public async Task<Result<IReadOnlyCollection<FeatureResponse>>> Handle(
        GetFeaturesQuery request, CancellationToken cancellationToken)
    {
        var features = await featureRepository.GetAllAsync(cancellationToken);

        IReadOnlyCollection<FeatureResponse> response = features
            .OrderBy(f => f.Id)
            .Select(f => new FeatureResponse(f.Id, f.Code, f.Name))
            .ToList();

        return Result.Success(response);
    }
}
