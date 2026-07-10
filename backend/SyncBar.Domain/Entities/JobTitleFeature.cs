using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class JobTitleFeature : Entity
{
    public long JobTitleId { get; private set; }
    public long AppFeatureId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private JobTitleFeature() : base(0) { }

    private JobTitleFeature(long jobTitleId, long appFeatureId) : base(0)
    {
        JobTitleId = jobTitleId;
        AppFeatureId = appFeatureId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<JobTitleFeature> Create(long jobTitleId, long appFeatureId)
    {
        if (jobTitleId <= 0 || appFeatureId <= 0)
            return Result.Failure<JobTitleFeature>(new Error("JobTitleFeature.InvalidIds", "Ids must be greater than zero."));

        return Result.Success(new JobTitleFeature(jobTitleId, appFeatureId));
    }

    public void Reactivate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
