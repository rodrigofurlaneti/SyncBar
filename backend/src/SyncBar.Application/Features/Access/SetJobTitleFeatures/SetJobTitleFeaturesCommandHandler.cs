using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Access.SetJobTitleFeatures;

internal sealed class SetJobTitleFeaturesCommandHandler(
    IJobTitleRepository jobTitleRepository,
    IJobTitleFeatureRepository jobTitleFeatureRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<SetJobTitleFeaturesCommand, Result>(logRepository, unitOfWork)
{
    public override async Task<Result<Result>> Handle(SetJobTitleFeaturesCommand request, CancellationToken cancellationToken)
    {
        var result = await ExecuteWithLogAsync(nameof(SetJobTitleFeaturesCommandHandler), nameof(Handle), null, async (userIdBox) =>
        {
            var jobTitle = await jobTitleRepository.GetByIdAsync(request.JobTitleId, cancellationToken);
            if (jobTitle is null || !jobTitle.IsActive)
                return Result.Failure<Result>(new Error("JobTitle.NotFound", "Job title not found."));

            var desired = request.FeatureIds.Distinct().ToHashSet();
            var links = await jobTitleFeatureRepository.GetByJobTitleForUpdateAsync(request.JobTitleId, cancellationToken);

            foreach (var link in links.Where(l => l.IsActive && !desired.Contains(l.AppFeatureId)))
                link.Deactivate();

            foreach (var link in links.Where(l => !l.IsActive && desired.Contains(l.AppFeatureId)))
                link.Reactivate();

            var existing = links.Select(l => l.AppFeatureId).ToHashSet();
            foreach (var featureId in desired.Except(existing))
            {
                var link = JobTitleFeature.Create(request.JobTitleId, featureId);
                if (link.IsFailure)
                    return Result.Failure<Result>(link.Error);
                await jobTitleFeatureRepository.AddAsync(link.Value, cancellationToken);
            }

            await unitOfWork.CommitAsync(cancellationToken);
            return Result.Success<Result>(Result.Success());
        });

        return result;
    }
}