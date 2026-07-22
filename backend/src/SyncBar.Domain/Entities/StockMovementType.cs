using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class StockMovementType : Entity
{
    public string Name { get; private set; } = null!;
    public bool IsInflow { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private StockMovementType() : base(0) { }

    private StockMovementType(string name, bool isInflow) : base(0)
    {
        Name = name;
        IsInflow = isInflow;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<StockMovementType> Create(string name, bool isInflow)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<StockMovementType>(new Error("StockMovementType.EmptyName", "Name is required."));
        return Result.Success(new StockMovementType(name, isInflow));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
