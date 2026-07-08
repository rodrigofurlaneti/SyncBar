using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class TableStatus : Entity
{
    public string Name { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private TableStatus() : base(0) { }

    private TableStatus(string name) : base(0)
    {
        Name = name;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<TableStatus> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<TableStatus>(new Error("TableStatus.EmptyName", "Name is required."));
        return Result.Success(new TableStatus(name));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
