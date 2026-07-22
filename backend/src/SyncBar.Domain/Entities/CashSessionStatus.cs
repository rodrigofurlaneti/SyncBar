using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class CashSessionStatus : Entity
{
    public string Name { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private CashSessionStatus() : base(0) { }

    private CashSessionStatus(string name) : base(0)
    {
        Name = name;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<CashSessionStatus> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<CashSessionStatus>(new Error("CashSessionStatus.EmptyName", "Name is required."));
        return Result.Success(new CashSessionStatus(name));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
