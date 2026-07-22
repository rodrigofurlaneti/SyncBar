using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class Permission : Entity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string ModuleName { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Permission() : base(0) { }

    private Permission(string code, string name, string moduleName) : base(0)
    {
        Code = code;
        Name = name;
        ModuleName = moduleName;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<Permission> Create(string code, string name, string moduleName)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<Permission>(new Error("Permission.EmptyCode", "Code is required."));
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Permission>(new Error("Permission.EmptyName", "Name is required."));
        if (string.IsNullOrWhiteSpace(moduleName))
            return Result.Failure<Permission>(new Error("Permission.EmptyModuleName", "ModuleName is required."));
        return Result.Success(new Permission(code, name, moduleName));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
