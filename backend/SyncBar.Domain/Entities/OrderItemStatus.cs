using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class OrderItemStatus : Entity
{
    public string Name { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private OrderItemStatus() : base(0) { }

    private OrderItemStatus(string name) : base(0)
    {
        Name = name;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<OrderItemStatus> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<OrderItemStatus>(new Error("OrderItemStatus.EmptyName", "Name is required."));
        return Result.Success(new OrderItemStatus(name));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
