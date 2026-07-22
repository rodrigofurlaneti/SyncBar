using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class ComandaStatus : Entity
{
    public string Name { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private ComandaStatus() : base(0) { }

    private ComandaStatus(string name) : base(0)
    {
        Name = name;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<ComandaStatus> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<ComandaStatus>(new Error("ComandaStatus.EmptyName", "Name is required."));
        return Result.Success(new ComandaStatus(name));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
