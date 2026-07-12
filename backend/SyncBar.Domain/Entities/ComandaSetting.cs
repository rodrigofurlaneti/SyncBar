using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class ComandaSetting : AggregateRoot
{
    public long BranchId { get; private set; }
    public decimal DefaultLimitAmount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private ComandaSetting() : base(0) { }

    private ComandaSetting(long branchId, decimal defaultLimitAmount) : base(0)
    {
        BranchId = branchId;
        DefaultLimitAmount = defaultLimitAmount;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<ComandaSetting> Create(long branchId, decimal defaultLimitAmount)
    {
        if (defaultLimitAmount <= 0)
            return Result.Failure<ComandaSetting>(new Error("ComandaSetting.InvalidLimit", "Limit must be greater than zero."));

        return Result.Success(new ComandaSetting(branchId, defaultLimitAmount));
    }

    public Result Update(decimal defaultLimitAmount)
    {
        if (defaultLimitAmount <= 0)
            return Result.Failure(new Error("ComandaSetting.InvalidLimit", "Limit must be greater than zero."));

        DefaultLimitAmount = defaultLimitAmount;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
