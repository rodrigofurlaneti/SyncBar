using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class OperatingCost : AggregateRoot
{
    public long BranchId { get; private set; }
    public long CostTypeId { get; private set; }
    public string Description { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public int ReferenceYear { get; private set; }
    public int ReferenceMonth { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private OperatingCost() : base(0) { }

    private OperatingCost(long branchId, long costTypeId, string description, decimal amount,
        int referenceYear, int referenceMonth) : base(0)
    {
        BranchId = branchId;
        CostTypeId = costTypeId;
        Description = description;
        Amount = amount;
        ReferenceYear = referenceYear;
        ReferenceMonth = referenceMonth;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<OperatingCost> Create(long branchId, long costTypeId, string description,
        decimal amount, int referenceYear, int referenceMonth)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure<OperatingCost>(new Error("OperatingCost.EmptyDescription", "Description is required."));
        if (amount <= 0)
            return Result.Failure<OperatingCost>(new Error("OperatingCost.InvalidAmount", "Amount must be greater than zero."));
        if (referenceMonth is < 1 or > 12)
            return Result.Failure<OperatingCost>(new Error("OperatingCost.InvalidMonth", "Reference month must be between 1 and 12."));
        if (referenceYear is < 2000 or > 2100)
            return Result.Failure<OperatingCost>(new Error("OperatingCost.InvalidYear", "Reference year out of range."));

        return Result.Success(new OperatingCost(branchId, costTypeId, description, amount, referenceYear, referenceMonth));
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
