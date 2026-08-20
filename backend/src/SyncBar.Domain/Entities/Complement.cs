using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Uma opção dentro de um ComplementGroup (ex.: dentro do grupo "Escolha uma bebida", cada
// Complement é uma bebida específica) — aponta pra um ComplementItem (cadastro leve, ver
// comentário lá) e carrega o preço adicional específico desse grupo (o mesmo ComplementItem
// pode, em teoria, ter preços diferentes em grupos diferentes). Entity filha de ComplementGroup
// — mesmo padrão de OrderItem filho de CustomerOrder.
public sealed class Complement : Entity
{
    public long ComplementGroupId { get; private set; }
    public long ComplementItemId { get; private set; }
    public decimal ExtraPrice { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Complement() : base(0) { }

    private Complement(long complementGroupId, long complementItemId, decimal extraPrice) : base(0)
    {
        ComplementGroupId = complementGroupId;
        ComplementItemId = complementItemId;
        ExtraPrice = extraPrice;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    internal static Result<Complement> Create(long complementGroupId, long complementItemId, decimal extraPrice)
    {
        if (extraPrice < 0)
            return Result.Failure<Complement>(new Error("Complement.InvalidExtraPrice", "Extra price cannot be negative."));

        return Result.Success(new Complement(complementGroupId, complementItemId, extraPrice));
    }

    internal Result UpdateExtraPrice(decimal extraPrice)
    {
        if (extraPrice < 0)
            return Result.Failure(new Error("Complement.InvalidExtraPrice", "Extra price cannot be negative."));

        ExtraPrice = extraPrice;
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    internal void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}
