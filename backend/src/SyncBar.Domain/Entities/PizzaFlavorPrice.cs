using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Fase 17 — o preço de um PizzaFlavor (sabor) num PizzaSize (tamanho) específico, dentro de uma
// PizzaConfiguration — espelha toppings[].prices[sizeId] da API de pizza do iFood (vínculo à
// categoria). A EXISTÊNCIA de uma linha aqui é o que torna o sabor "vendável" naquele tamanho —
// não há uma tabela de vínculo separada: se não tem preço para o tamanho, o sabor não aparece
// como opção para aquele tamanho (mesma decisão que evita uma entidade extra sem necessidade).
public sealed class PizzaFlavorPrice : Entity
{
    public long PizzaConfigurationId { get; private set; }
    public long PizzaFlavorId { get; private set; }
    public long PizzaSizeId { get; private set; }
    public decimal Price { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private PizzaFlavorPrice() : base(0) { }

    private PizzaFlavorPrice(long pizzaConfigurationId, long pizzaFlavorId, long pizzaSizeId, decimal price) : base(0)
    {
        PizzaConfigurationId = pizzaConfigurationId;
        PizzaFlavorId = pizzaFlavorId;
        PizzaSizeId = pizzaSizeId;
        Price = price;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    internal static Result<PizzaFlavorPrice> Create(long pizzaConfigurationId, long pizzaFlavorId, long pizzaSizeId, decimal price)
    {
        if (price < 0)
            return Result.Failure<PizzaFlavorPrice>(new Error("PizzaFlavorPrice.InvalidPrice", "Price cannot be negative."));

        return Result.Success(new PizzaFlavorPrice(pizzaConfigurationId, pizzaFlavorId, pizzaSizeId, price));
    }

    internal Result UpdatePrice(decimal price)
    {
        if (price < 0)
            return Result.Failure(new Error("PizzaFlavorPrice.InvalidPrice", "Price cannot be negative."));

        Price = price;
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    internal void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}
