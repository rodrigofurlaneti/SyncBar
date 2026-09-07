using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class OrderOrigin : AggregateRoot
{
    public long? CompanyId { get; private set; }
    public long? BranchId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private OrderOrigin() : base(0) { }

    private OrderOrigin(long? companyId, long? branchId, string name, DateTime now) : base(0)
    {
        CompanyId = companyId;
        BranchId = branchId;
        Name = name;
        CreatedAt = now;
        IsActive = true;
    }

    public static Result<OrderOrigin> Create(long? companyId, long? branchId, string name, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<OrderOrigin>(new Error("OrderOrigin.EmptyName", "Order origin name cannot be empty."));

        return Result.Success(new OrderOrigin(companyId, branchId, name.Trim().ToUpperInvariant(), now));
    }
}