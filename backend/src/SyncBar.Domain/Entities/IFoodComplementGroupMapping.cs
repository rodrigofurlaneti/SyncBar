using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Liga um ComplementGroup do SyncBar ao optionGroup correspondente no catálogo do Ifood, por
// FILIAL — mesmo padrão de IfoodProductMapping/IfoodCategoryMapping (catálogo Ifood é por
// merchant). IfoodOptionGroupId é um GUID gerado por nós (Ifood exige UUID v4 no campo `id`
// do optionGroup) e persistido aqui pra todo PUT de optionGroups seguinte ser idempotente.
public sealed class IfoodComplementGroupMapping : AggregateRoot
{
    public long ComplementGroupId { get; private set; }
    public long BranchId { get; private set; }
    public Guid IfoodOptionGroupId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; }
    public bool IsActive { get; private set; }

    private IfoodComplementGroupMapping() : base(0) { }

    private IfoodComplementGroupMapping(long complementGroupId, long branchId, Guid ifoodOptionGroupId) : base(0)
    {
        ComplementGroupId = complementGroupId;
        BranchId = branchId;
        IfoodOptionGroupId = ifoodOptionGroupId;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<IfoodComplementGroupMapping> Create(long complementGroupId, long branchId)
        => Result.Success(new IfoodComplementGroupMapping(complementGroupId, branchId, Guid.NewGuid()));
}
