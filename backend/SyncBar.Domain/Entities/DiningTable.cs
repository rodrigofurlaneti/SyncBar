using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class DiningTable : AggregateRoot
{
    public long BranchId { get; private set; }
    public long TableStatusId { get; private set; }
    public int Number { get; private set; }
    public int? Capacity { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private DiningTable() : base(0) { }

    private DiningTable(long branchId, long tableStatusId, int number, int? capacity) : base(0)
    {
        BranchId = branchId;
        TableStatusId = tableStatusId;
        Number = number;
        Capacity = capacity;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<DiningTable> Create(long branchId, long tableStatusId, int number, int? capacity)
    {
        // No required-string invariants for this entity.
        return Result.Success(new DiningTable(branchId, tableStatusId, number, capacity));
    }

    public void ChangeStatus(long tableStatusId)
    {
        TableStatusId = tableStatusId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
