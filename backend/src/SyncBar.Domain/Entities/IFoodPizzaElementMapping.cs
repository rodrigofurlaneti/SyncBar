using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Fase 17 — filha de IfoodPizzaMapping. Guarda o id de um elemento específico (size/crust/edge/
// topping) devolvido pelo Ifood na criação/atualização da pizza — ver IfoodPizzaElementKind pros
// valores de Kind e comentário completo em IfoodPizzaMapping.
public sealed class IfoodPizzaElementMapping : Entity
{
    public long IfoodPizzaMappingId { get; private set; }
    public byte Kind { get; private set; }
    public long LocalId { get; private set; }
    public string IfoodElementId { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IfoodPizzaElementMapping() : base(0) { }

    private IfoodPizzaElementMapping(long IfoodPizzaMappingId, byte kind, long localId, string IfoodElementId) : base(0)
    {
        IfoodPizzaMappingId = IfoodPizzaMappingId;
        Kind = kind;
        LocalId = localId;
        IfoodElementId = IfoodElementId;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    internal static IfoodPizzaElementMapping Create(long IfoodPizzaMappingId, byte kind, long localId, string IfoodElementId) =>
        new(IfoodPizzaMappingId, kind, localId, IfoodElementId);

    internal void UpdateIfoodElementId(string IfoodElementId)
    {
        IfoodElementId = IfoodElementId;
        UpdatedAt = DateTime.Now;
    }
}
