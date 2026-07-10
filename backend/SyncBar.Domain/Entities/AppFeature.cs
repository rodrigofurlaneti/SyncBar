using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class AppFeature : Entity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private AppFeature() : base(0) { }

    private AppFeature(string code, string name) : base(0)
    {
        Code = code;
        Name = name;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<AppFeature> Create(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<AppFeature>(new Error("AppFeature.EmptyCode", "Code is required."));
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<AppFeature>(new Error("AppFeature.EmptyName", "Name is required."));

        return Result.Success(new AppFeature(code, name));
    }
}
