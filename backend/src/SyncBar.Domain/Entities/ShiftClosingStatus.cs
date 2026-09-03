using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Lookup somente leitura, mesmo padrão de CashSessionStatus — ids fixos em
// ShiftClosingStatusIds (Domain/Constants/LookupIds.cs), seedados em sql.
public sealed class ShiftClosingStatus : Entity
{
    public string Name { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private ShiftClosingStatus() : base(0) { }

    private ShiftClosingStatus(string name) : base(0)
    {
        Name = name;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<ShiftClosingStatus> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<ShiftClosingStatus>(new Error("ShiftClosingStatus.EmptyName", "Name is required."));

        return Result.Success(new ShiftClosingStatus(name));
    }

    public void Touch() => UpdatedAt = DateTime.Now;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}
