using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class CashMovement : Entity
{
    public long CashSessionId { get; private set; }
    public long CashMovementTypeId { get; private set; }
    public long? SaleId { get; private set; }
    public long EmployeeId { get; private set; }
    public decimal Amount { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private CashMovement() : base(0) { }

    private CashMovement(long cashSessionId, long cashMovementTypeId, long? saleId, long employeeId, decimal amount, string? description) : base(0)
    {
        CashSessionId = cashSessionId;
        CashMovementTypeId = cashMovementTypeId;
        SaleId = saleId;
        EmployeeId = employeeId;
        Amount = amount;
        Description = description;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<CashMovement> Create(long cashSessionId, long cashMovementTypeId, long? saleId, long employeeId, decimal amount, string? description)
    {
        // No required-string invariants for this entity.
        return Result.Success(new CashMovement(cashSessionId, cashMovementTypeId, saleId, employeeId, amount, description));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
