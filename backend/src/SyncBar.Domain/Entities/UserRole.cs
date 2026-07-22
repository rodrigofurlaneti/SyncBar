using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class UserRole : Entity
{
    public long AppUserId { get; private set; }
    public long RoleId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private UserRole() : base(0) { }

    private UserRole(long appUserId, long roleId) : base(0)
    {
        AppUserId = appUserId;
        RoleId = roleId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<UserRole> Create(long appUserId, long roleId)
    {
        // No required-string invariants for this entity.
        return Result.Success(new UserRole(appUserId, roleId));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
