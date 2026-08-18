using SyncBar.Domain.Primitives;

public sealed class UserRole : Entity
{
    public long CompanyId { get; private set; }
    public long AppUserId { get; private set; }
    public long RoleId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private UserRole() : base(0) { }

    private UserRole(long companyId, long appUserId, long roleId) : base(0)
    {
        CompanyId = companyId;
        AppUserId = appUserId;
        RoleId = roleId;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<UserRole> Create(long companyId, long appUserId, long roleId)
    {
        return Result.Success(new UserRole(companyId, appUserId, roleId));
    }

    public void Touch() => UpdatedAt = DateTime.Now;
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}