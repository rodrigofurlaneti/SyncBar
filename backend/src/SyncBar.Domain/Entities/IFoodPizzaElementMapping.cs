using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Fase 17 — filha de IFoodPizzaMapping. Guarda o id de um elemento específico (size/crust/edge/
// topping) devolvido pelo iFood na criação/atualização da pizza — ver IFoodPizzaElementKind pros
// valores de Kind e comentário completo em IFoodPizzaMapping.
public sealed class IFoodPizzaElementMapping : Entity
{
    public long IFoodPizzaMappingId { get; private set; }
    public byte Kind { get; private set; }
    public long LocalId { get; private set; }
    public string IFoodElementId { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IFoodPizzaElementMapping() : base(0) { }

    private IFoodPizzaElementMapping(long ifoodPizzaMappingId, byte kind, long localId, string ifoodElementId) : base(0)
    {
        IFoodPizzaMappingId = ifoodPizzaMappingId;
        Kind = kind;
        LocalId = localId;
        IFoodElementId = ifoodElementId;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    internal static IFoodPizzaElementMapping Create(long ifoodPizzaMappingId, byte kind, long localId, string ifoodElementId) =>
        new(ifoodPizzaMappingId, kind, localId, ifoodElementId);

    internal void UpdateIFoodElementId(string ifoodElementId)
    {
        IFoodElementId = ifoodElementId;
        UpdatedAt = DateTime.Now;
    }
}
