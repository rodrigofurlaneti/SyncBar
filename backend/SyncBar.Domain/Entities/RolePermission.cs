using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class RolePermission : Entity
{
    public long RoleId { get; private set; }
    public long PermissionId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private RolePermission() : base(0) { }

    private RolePermission(long roleId, long permissionId) : base(0)
    {
        RoleId = roleId;
        PermissionId = permissionId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<RolePermission> Create(long roleId, long permissionId)
    {
        // No required-string invariants for this entity.
        return Result.Success(new RolePermission(roleId, permissionId));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
