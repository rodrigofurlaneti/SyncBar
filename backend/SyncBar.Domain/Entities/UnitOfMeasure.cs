using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class UnitOfMeasure : Entity
{
    public string Name { get; private set; } = null!;
    public string Abbreviation { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private UnitOfMeasure() : base(0) { }

    private UnitOfMeasure(string name, string abbreviation) : base(0)
    {
        Name = name;
        Abbreviation = abbreviation;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<UnitOfMeasure> Create(string name, string abbreviation)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<UnitOfMeasure>(new Error("UnitOfMeasure.EmptyName", "Name is required."));
        if (string.IsNullOrWhiteSpace(abbreviation))
            return Result.Failure<UnitOfMeasure>(new Error("UnitOfMeasure.EmptyAbbreviation", "Abbreviation is required."));
        return Result.Success(new UnitOfMeasure(name, abbreviation));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
