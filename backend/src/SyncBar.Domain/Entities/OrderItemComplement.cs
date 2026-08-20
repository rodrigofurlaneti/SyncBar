using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Complemento efetivamente escolhido dentro de um OrderItem (ex.: o cliente pediu o
// hambúrguer com bacon extra) — filha de OrderItem, mesmo padrão de OrderItem filho de
// CustomerOrder. UnitPriceCharged é o preço adicional CONGELADO no momento do lançamento
// (não recalcula do ComplementGroup.Complement — mesma regra de OrderItem.UnitPrice
// congelado do Product). Simplificação assumida na Fase 6a: é um valor fixo por linha do
// pedido, não multiplicado pela Quantity do OrderItem pai.
public sealed class OrderItemComplement : Entity
{
    public long OrderItemId { get; private set; }
    public long ComplementId { get; private set; }
    public decimal UnitPriceCharged { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private OrderItemComplement() : base(0) { }

    private OrderItemComplement(long orderItemId, long complementId, decimal unitPriceCharged, DateTime Now) : base(0)
    {
        OrderItemId = orderItemId;
        ComplementId = complementId;
        UnitPriceCharged = unitPriceCharged;
        IsActive = true;
        CreatedAt = Now;
    }

    internal static Result<OrderItemComplement> Create(long orderItemId, long complementId, decimal unitPriceCharged, DateTime Now)
    {
        if (unitPriceCharged < 0)
            return Result.Failure<OrderItemComplement>(new Error("OrderItemComplement.InvalidPrice", "Price charged cannot be negative."));

        return Result.Success(new OrderItemComplement(orderItemId, complementId, unitPriceCharged, Now));
    }

    internal void Deactivate(DateTime Now)
    {
        IsActive = false;
        UpdatedAt = Now;
    }
}
