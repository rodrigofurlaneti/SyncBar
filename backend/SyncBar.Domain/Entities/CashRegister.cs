using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class CashRegister : AggregateRoot
{
    public long BranchId { get; private set; }
    public string Name { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private CashRegister() : base(0) { }

    private CashRegister(long branchId, string name) : base(0)
    {
        BranchId = branchId;
        Name = name;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<CashRegister> Create(long branchId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<CashRegister>(new Error("CashRegister.EmptyName", "Name is required."));
        return Result.Success(new CashRegister(branchId, name));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
