using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class DiningTable : AggregateRoot
{
    public long BranchId { get; private set; }
    public long TableStatusId { get; private set; }
    public int Number { get; private set; }
    public int? Capacity { get; private set; }
    public Guid? QrToken { get; private set; }
    public bool IsQrViewEnabled { get; private set; }
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
        IsQrViewEnabled = true; // Por padrão, começa ligada
        CreatedAt = DateTime.Now;
    }

    public static Result<DiningTable> Create(long branchId, long tableStatusId, int number, int? capacity)
    {
        return Result.Success(new DiningTable(branchId, tableStatusId, number, capacity));
    }

    public void ChangeStatus(long tableStatusId)
    {
        TableStatusId = tableStatusId;
        UpdatedAt = DateTime.Now;
    }

    public Guid GenerateQrToken()
    {
        QrToken = Guid.NewGuid();
        UpdatedAt = DateTime.Now;
        return QrToken.Value;
    }

    public void SetQrViewEnabled(bool enabled)
    {
        IsQrViewEnabled = enabled;
        UpdatedAt = DateTime.Now;
    }

    public void Touch() => UpdatedAt = DateTime.Now;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}