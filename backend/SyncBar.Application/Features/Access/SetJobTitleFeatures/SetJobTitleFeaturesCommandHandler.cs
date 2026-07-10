using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Access.SetJobTitleFeatures;

internal sealed class SetJobTitleFeaturesCommandHandler(
    IJobTitleRepository jobTitleRepository,
    IJobTitleFeatureRepository jobTitleFeatureRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetJobTitleFeaturesCommand>
{
    public async Task<Result> Handle(SetJobTitleFeaturesCommand request, CancellationToken cancellationToken)
    {
        var jobTitle = await jobTitleRepository.GetByIdAsync(request.JobTitleId, cancellationToken);
        if (jobTitle is null || !jobTitle.IsActive)
            return Result.Failure(new Error("JobTitle.NotFound", "Job title not found."));

        var desired = request.FeatureIds.Distinct().ToHashSet();
        var links = await jobTitleFeatureRepository.GetByJobTitleForUpdateAsync(request.JobTitleId, cancellationToken);

        // Soft delete: remove desativando; reaproveita vinculo inativo ao reconceder.
        foreach (var link in links.Where(l => l.IsActive && !desired.Contains(l.AppFeatureId)))
            link.Deactivate();

        foreach (var link in links.Where(l => !l.IsActive && desired.Contains(l.AppFeatureId)))
            link.Reactivate();

        var existing = links.Select(l => l.AppFeatureId).ToHashSet();
        foreach (var featureId in desired.Except(existing))
        {
            var link = JobTitleFeature.Create(request.JobTitleId, featureId);
            if (link.IsFailure)
                return Result.Failure(link.Error);
            await jobTitleFeatureRepository.AddAsync(link.Value, cancellationToken);
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
