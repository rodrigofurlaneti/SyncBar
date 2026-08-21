using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Liga um ComplementGroup do SyncBar ao optionGroup correspondente no catálogo do iFood, por
// FILIAL — mesmo padrão de IFoodProductMapping/IFoodCategoryMapping (catálogo iFood é por
// merchant). IFoodOptionGroupId é um GUID gerado por nós (iFood exige UUID v4 no campo `id`
// do optionGroup) e persistido aqui pra todo PUT de optionGroups seguinte ser idempotente.
public sealed class IFoodComplementGroupMapping : AggregateRoot
{
    public long ComplementGroupId { get; private set; }
    public long BranchId { get; private set; }
    public Guid IFoodOptionGroupId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IFoodComplementGroupMapping() : base(0) { }

    private IFoodComplementGroupMapping(long complementGroupId, long branchId, Guid ifoodOptionGroupId) : base(0)
    {
        ComplementGroupId = complementGroupId;
        BranchId = branchId;
        IFoodOptionGroupId = ifoodOptionGroupId;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<IFoodComplementGroupMapping> Create(long complementGroupId, long branchId)
        => Result.Success(new IFoodComplementGroupMapping(complementGroupId, branchId, Guid.NewGuid()));
}
