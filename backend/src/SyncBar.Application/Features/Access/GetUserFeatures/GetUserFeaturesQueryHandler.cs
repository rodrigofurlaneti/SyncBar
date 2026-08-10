using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Access.GetUserFeatures;

internal sealed class GetUserFeaturesQueryHandler(
    IAppUserFeatureRepository userFeatureRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetUserFeaturesQuery, IReadOnlyCollection<long>>(logRepository, unitOfWork)
{
    public override Task<Result<IReadOnlyCollection<long>>> Handle(
        GetUserFeaturesQuery request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(nameof(GetUserFeaturesQueryHandler), nameof(Handle), null, async (userIdBox) =>
        {
            var links = await userFeatureRepository.GetByUserAsync(request.AppUserId, cancellationToken);
            IReadOnlyCollection<long> response = links.Select(l => l.AppFeatureId).ToList();
            return Result.Success(response);
        });
}