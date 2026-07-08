using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class Role : AggregateRoot
{
    public long CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Role() : base(0) { }

    private Role(long companyId, string name, string? description) : base(0)
    {
        CompanyId = companyId;
        Name = name;
        Description = description;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<Role> Create(long companyId, string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Role>(new Error("Role.EmptyName", "Name is required."));
        return Result.Success(new Role(companyId, name, description));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
