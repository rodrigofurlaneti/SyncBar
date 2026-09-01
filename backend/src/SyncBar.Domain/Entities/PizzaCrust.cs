using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Fase 17 — uma borda (ex.: "Borda Fina", "Borda Grossa") dentro de uma PizzaConfiguration —
// espelha crusts[] da API de pizza do Ifood. ExtraPrice é o preço único do crust (não varia por
// tamanho — mesma simplificação usada pelo Ifood no payload de vínculo à categoria, já que o
// SyncBar tem um catálogo por filial).
public sealed class PizzaCrust : Entity
{
    public long PizzaConfigurationId { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal ExtraPrice { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private PizzaCrust() : base(0) { }

    private PizzaCrust(long pizzaConfigurationId, string name, decimal extraPrice, int displayOrder) : base(0)
    {
        PizzaConfigurationId = pizzaConfigurationId;
        Name = name;
        ExtraPrice = extraPrice;
        DisplayOrder = displayOrder;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    internal static Result<PizzaCrust> Create(long pizzaConfigurationId, string name, decimal extraPrice, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<PizzaCrust>(new Error("PizzaCrust.EmptyName", "Name is required."));
        if (extraPrice < 0)
            return Result.Failure<PizzaCrust>(new Error("PizzaCrust.InvalidExtraPrice", "Extra price cannot be negative."));

        return Result.Success(new PizzaCrust(pizzaConfigurationId, name, extraPrice, displayOrder));
    }

    internal Result UpdateDetails(string name, decimal extraPrice, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(new Error("PizzaCrust.EmptyName", "Name is required."));
        if (extraPrice < 0)
            return Result.Failure(new Error("PizzaCrust.InvalidExtraPrice", "Extra price cannot be negative."));

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
