using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class PaymentMethod : Entity
{
    public string Name { get; private set; } = null!;
    public bool AllowsChange { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private PaymentMethod() : base(0) { }

    private PaymentMethod(string name, bool allowsChange) : base(0)
    {
        Name = name;
        AllowsChange = allowsChange;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<PaymentMethod> Create(string name, bool allowsChange)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<PaymentMethod>(new Error("PaymentMethod.EmptyName", "Name is required."));
        return Result.Success(new PaymentMethod(name, allowsChange));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
