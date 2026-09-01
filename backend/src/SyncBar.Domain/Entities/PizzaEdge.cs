using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Fase 17 — uma "edge" / recheio de borda (ex.: "Catupiry", "Cheddar") dentro de uma
// PizzaConfiguration — espelha edges[] da API de pizza do Ifood. Mesma simplificação de
// PizzaCrust: ExtraPrice único, não varia por tamanho.
public sealed class PizzaEdge : Entity
{
    public long PizzaConfigurationId { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal ExtraPrice { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private PizzaEdge() : base(0) { }

    private PizzaEdge(long pizzaConfigurationId, string name, decimal extraPrice, int displayOrder) : base(0)
    {
        PizzaConfigurationId = pizzaConfigurationId;
        Name = name;
        ExtraPrice = extraPrice;
        DisplayOrder = displayOrder;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    internal static Result<PizzaEdge> Create(long pizzaConfigurationId, string name, decimal extraPrice, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<PizzaEdge>(new Error("PizzaEdge.EmptyName", "Name is required."));
        if (extraPrice < 0)
            return Result.Failure<PizzaEdge>(new Error("PizzaEdge.InvalidExtraPrice", "Extra price cannot be negative."));

        return Result.Success(new PizzaEdge(pizzaConfigurationId, name, extraPrice, displayOrder));
    }

    internal Result UpdateDetails(string name, decimal extraPrice, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(new Error("PizzaEdge.EmptyName", "Name is required."));
        if (extraPrice < 0)
            return Result.Failure(new Error("PizzaEdge.InvalidExtraPrice", "Extra price cannot be negative."));

        Name = name;
        ExtraPrice = extraPrice;
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
