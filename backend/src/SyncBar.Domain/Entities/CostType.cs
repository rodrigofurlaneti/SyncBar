using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class CostType : Entity
{
    public string Name { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private CostType() : base(0) { }

    private CostType(string name) : base(0)
    {
        Name = name;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<CostType> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<CostType>(new Error("CostType.EmptyName", "Name is required."));
        return Result.Success(new CostType(name));
    }

    public void Touch() => UpdatedAt = DateTime.Now;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}
