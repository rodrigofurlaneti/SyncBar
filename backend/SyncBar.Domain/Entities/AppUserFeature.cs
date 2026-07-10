using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class AppUserFeature : Entity
{
    public long AppUserId { get; private set; }
    public long AppFeatureId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private AppUserFeature() : base(0) { }

    private AppUserFeature(long appUserId, long appFeatureId) : base(0)
    {
        AppUserId = appUserId;
        AppFeatureId = appFeatureId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<AppUserFeature> Create(long appUserId, long appFeatureId)
    {
        if (appUserId <= 0 || appFeatureId <= 0)
            return Result.Failure<AppUserFeature>(new Error("AppUserFeature.InvalidIds", "Ids must be greater than zero."));

        return Result.Success(new AppUserFeature(appUserId, appFeatureId));
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
