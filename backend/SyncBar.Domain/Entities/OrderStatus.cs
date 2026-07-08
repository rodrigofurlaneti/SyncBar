using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class OrderStatus : Entity
{
    public string Name { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private OrderStatus() : base(0) { }

    private OrderStatus(string name) : base(0)
    {
        Name = name;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<OrderStatus> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<OrderStatus>(new Error("OrderStatus.EmptyName", "Name is required."));
        return Result.Success(new OrderStatus(name));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
