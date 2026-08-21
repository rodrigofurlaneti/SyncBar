using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Liga um Complement (opção dentro de um ComplementGroup) do SyncBar ao option correspondente
// no catálogo do iFood, por FILIAL — mesmo padrão de IFoodProductMapping. Assim como um "item"
// do iFood embrulha um "product", cada "option" de um optionGroup também embrulha o seu próprio
// objeto "product" (mesma hierarquia da Catalog API) — por isso IFoodOptionId (id da option) e
// IFoodProductId (id do product embrulhado pela option) são GUIDs distintos, gerados por nós.
public sealed class IFoodComplementMapping : AggregateRoot
{
    public long ComplementId { get; private set; }
    public long BranchId { get; private set; }
    public Guid IFoodOptionId { get; private set; }
    public Guid IFoodProductId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IFoodComplementMapping() : base(0) { }

    private IFoodComplementMapping(long complementId, long branchId, Guid ifoodOptionId, Guid ifoodProductId) : base(0)
    {
        ComplementId = complementId;
        BranchId = branchId;
        IFoodOptionId = ifoodOptionId;
        IFoodProductId = ifoodProductId;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<IFoodComplementMapping> Create(long complementId, long branchId)
        => Result.Success(new IFoodComplementMapping(complementId, branchId, Guid.NewGuid(), Guid.NewGuid()));
}
