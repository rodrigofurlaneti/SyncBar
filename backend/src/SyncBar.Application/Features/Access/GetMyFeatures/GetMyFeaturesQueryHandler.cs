using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Access.GetFeatures;

internal sealed class GetFeaturesQueryHandler(
    IAppFeatureRepository featureRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetFeaturesQuery, IReadOnlyCollection<FeatureResponse>>(logRepository, unitOfWork)
{
    public override Task<Result<IReadOnlyCollection<FeatureResponse>>> Handle(
        GetFeaturesQuery request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(nameof(GetFeaturesQueryHandler), nameof(Handle), null, async (userIdBox) =>
        {
            var features = await featureRepository.GetAllAsync(cancellationToken);

            IReadOnlyCollection<FeatureResponse> response = features
                .OrderBy(f => f.Id)
                .Select(f => new FeatureResponse(f.Id, f.Code, f.Name))
                .ToList();

            return Result.Success(response);
        });
}