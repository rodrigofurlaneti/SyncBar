using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Access.SetUserFeatures;

internal sealed class SetUserFeaturesCommandHandler(
    IAppUserRepository userRepository,
    IAppUserFeatureRepository userFeatureRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetUserFeaturesCommand>
{
    public async Task<Result> Handle(SetUserFeaturesCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.AppUserId, cancellationToken);
        if (user is null || !user.IsActive)
            return Result.Failure(new Error("AppUser.NotFound", "User not found."));

        var desired = request.FeatureIds.Distinct().ToHashSet();
        var links = await userFeatureRepository.GetByUserForUpdateAsync(request.AppUserId, cancellationToken);

        foreach (var link in links.Where(l => l.IsActive && !desired.Contains(l.AppFeatureId)))
            link.Deactivate();

        foreach (var link in links.Where(l => !l.IsActive && desired.Contains(l.AppFeatureId)))
            link.Reactivate();

        var existing = links.Select(l => l.AppFeatureId).ToHashSet();
        foreach (var featureId in desired.Except(existing))
        {
            var link = AppUserFeature.Create(request.AppUserId, featureId);
            if (link.IsFailure)
                return Result.Failure(link.Error);
            await userFeatureRepository.AddAsync(link.Value, cancellationToken);
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
