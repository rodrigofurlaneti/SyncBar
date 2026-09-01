using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Fase 17 — um tamanho de pizza (ex.: "Grande", "Broto") dentro de uma PizzaConfiguration —
// espelha sizes[] da API de pizza do Ifood (name/slices/acceptedFractions/index). Entity filha
// de PizzaConfiguration, mesmo padrão de Complement filho de ComplementGroup.
public sealed class PizzaSize : Entity
{
    public long PizzaConfigurationId { get; private set; }
    public string Name { get; private set; } = null!;
    public int? Slices { get; private set; }
    public int AcceptedFractions { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private PizzaSize() : base(0) { }

    private PizzaSize(long pizzaConfigurationId, string name, int? slices, int acceptedFractions, int displayOrder) : base(0)
    {
        PizzaConfigurationId = pizzaConfigurationId;
        Name = name;
        Slices = slices;
        AcceptedFractions = acceptedFractions;
        DisplayOrder = displayOrder;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    internal static Result<PizzaSize> Create(long pizzaConfigurationId, string name, int? slices, int acceptedFractions, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<PizzaSize>(new Error("PizzaSize.EmptyName", "Name is required."));
        if (acceptedFractions < 1 || acceptedFractions > 4)
            return Result.Failure<PizzaSize>(new Error("PizzaSize.InvalidAcceptedFractions", "Accepted fractions must be between 1 and 4."));

        return Result.Success(new PizzaSize(pizzaConfigurationId, name, slices, acceptedFractions, displayOrder));
    }

    internal Result UpdateDetails(string name, int? slices, int acceptedFractions, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(new Error("PizzaSize.EmptyName", "Name is required."));
        if (acceptedFractions < 1 || acceptedFractions > 4)
            return Result.Failure(new Error("PizzaSize.InvalidAcceptedFractions", "Accepted fractions must be between 1 and 4."));

        Name = name;
        Slices = slices;
        AcceptedFractions = acceptedFractions;
        DisplayOrder = displayOrder;
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    internal void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}
