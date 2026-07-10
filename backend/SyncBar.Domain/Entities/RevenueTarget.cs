using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class RevenueTarget : AggregateRoot
{
    public long BranchId { get; private set; }
    public int ReferenceYear { get; private set; }
    public int ReferenceMonth { get; private set; }
    public decimal TargetAmount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private RevenueTarget() : base(0) { }

    private RevenueTarget(long branchId, int referenceYear, int referenceMonth, decimal targetAmount) : base(0)
    {
        BranchId = branchId;
        ReferenceYear = referenceYear;
        ReferenceMonth = referenceMonth;
        TargetAmount = targetAmount;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<RevenueTarget> Create(long branchId, int referenceYear, int referenceMonth, decimal targetAmount)
    {
        if (targetAmount <= 0)
            return Result.Failure<RevenueTarget>(new Error("RevenueTarget.InvalidAmount", "Target must be greater than zero."));
        if (referenceMonth is < 1 or > 12)
            return Result.Failure<RevenueTarget>(new Error("RevenueTarget.InvalidMonth", "Reference month must be between 1 and 12."));
        if (referenceYear is < 2000 or > 2100)
            return Result.Failure<RevenueTarget>(new Error("RevenueTarget.InvalidYear", "Reference year out of range."));

        return Result.Success(new RevenueTarget(branchId, referenceYear, referenceMonth, targetAmount));
    }

    public Result UpdateAmount(decimal targetAmount)
    {
        if (targetAmount <= 0)
            return Result.Failure(new Error("RevenueTarget.InvalidAmount", "Target must be greater than zero."));

        TargetAmount = targetAmount;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
