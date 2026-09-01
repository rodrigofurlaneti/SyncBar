using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Liga um Complement (opção dentro de um ComplementGroup) do SyncBar ao option correspondente
// no catálogo do Ifood, por FILIAL — mesmo padrão de IfoodProductMapping. Assim como um "item"
// do Ifood embrulha um "product", cada "option" de um optionGroup também embrulha o seu próprio
// objeto "product" (mesma hierarquia da Catalog API) — por isso IfoodOptionId (id da option) e
// IfoodProductId (id do product embrulhado pela option) são GUIDs distintos, gerados por nós.
public sealed class IfoodComplementMapping : AggregateRoot
{
    public long ComplementId { get; private set; }
    public long BranchId { get; private set; }
    public Guid IfoodOptionId { get; private set; }
    public Guid IfoodProductId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; }
    public bool IsActive { get; private set; }

    private IfoodComplementMapping() : base(0) { }

    private IfoodComplementMapping(long complementId, long branchId, Guid IfoodOptionId, Guid IfoodProductId) : base(0)
    {
        ComplementId = complementId;
        BranchId = branchId;
        IfoodOptionId = IfoodOptionId;
        IfoodProductId = IfoodProductId;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<IfoodComplementMapping> Create(long complementId, long branchId)
        => Result.Success(new IfoodComplementMapping(complementId, branchId, Guid.NewGuid(), Guid.NewGuid()));
}
