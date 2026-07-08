using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class Comanda : AggregateRoot
{
    public long BranchId { get; private set; }
    public long ComandaStatusId { get; private set; }
    public string Code { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Comanda() : base(0) { }

    private Comanda(long branchId, long comandaStatusId, string code) : base(0)
    {
        BranchId = branchId;
        ComandaStatusId = comandaStatusId;
        Code = code;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<Comanda> Create(long branchId, long comandaStatusId, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<Comanda>(new Error("Comanda.EmptyCode", "Code is required."));
        return Result.Success(new Comanda(branchId, comandaStatusId, code));
    }

    public void ChangeStatus(long comandaStatusId)
    {
        ComandaStatusId = comandaStatusId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
