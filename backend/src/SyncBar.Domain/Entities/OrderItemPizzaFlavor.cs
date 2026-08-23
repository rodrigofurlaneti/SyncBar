using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Fase 17 — um sabor efetivamente escolhido dentro de um OrderItem de pizza (ex.: metade
// Calabresa, metade Frango Catupiry) — filha de OrderItem, mesmo padrão de OrderItemComplement.
// FractionShare é a fração da pizza ocupada por esse sabor (1 = pizza inteira, 0.5 = metade,
// 0.3334 ≈ um terço) — o valor não vem do cliente: OrderItem.CreatePizza divide 1 pelo número de
// sabores escolhidos, então a soma fecha em ~1 por construção (o arredondamento pra 4 casas pode
// deixar diferença de centésimos de milésimo, sem efeito porque a fração é informativa, não
// entra em preço). UnitPriceCharged aqui é sempre 0: o preço da pizza fracionada já foi decidido de
// uma vez (o sabor mais caro, ver PizzaConfiguration.CalculateUnitPrice) e está congelado em
// OrderItem.UnitPrice — este registro existe só para saber QUAIS sabores foram pedidos.
public sealed class OrderItemPizzaFlavor : Entity
{
    public long OrderItemId { get; private set; }
    public long PizzaFlavorId { get; private set; }
    public decimal FractionShare { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private OrderItemPizzaFlavor() : base(0) { }

    private OrderItemPizzaFlavor(long orderItemId, long pizzaFlavorId, decimal fractionShare, DateTime Now) : base(0)
    {
        OrderItemId = orderItemId;
        PizzaFlavorId = pizzaFlavorId;
        FractionShare = fractionShare;
        IsActive = true;
        CreatedAt = Now;
    }

    internal static Result<OrderItemPizzaFlavor> Create(long orderItemId, long pizzaFlavorId, decimal fractionShare, DateTime Now)
    {
        if (fractionShare <= 0 || fractionShare > 1)
            return Result.Failure<OrderItemPizzaFlavor>(new Error("OrderItemPizzaFlavor.InvalidFraction", "Fraction share must be between 0 (exclusive) and 1 (inclusive)."));

        return Result.Success(new OrderItemPizzaFlavor(orderItemId, pizzaFlavorId, fractionShare, Now));
    }

    internal void Deactivate(DateTime Now)
    {
        IsActive = false;
    }
}
