using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class CashMovementType : Entity
{
    public string Name { get; private set; } = null!;
    public bool IsInflow { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private CashMovementType() : base(0) { }

    private CashMovementType(string name, bool isInflow) : base(0)
    {
        Name = name;
        IsInflow = isInflow;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<CashMovementType> Create(string name, bool isInflow)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<CashMovementType>(new Error("CashMovementType.EmptyName", "Name is required."));
        return Result.Success(new CashMovementType(name, isInflow));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
