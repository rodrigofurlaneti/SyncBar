using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Liga uma Category do SyncBar à categoria correspondente no catálogo do iFood, por FILIAL —
// o catálogo do iFood é por merchant (por loja), então a mesma Category (que é por empresa)
// pode virar categorias diferentes em cada merchant. Criada uma vez (POST /categories) e
// reaproveitada em toda sincronização seguinte (PUT /items só referencia o IFoodCategoryId).
public sealed class IFoodCategoryMapping : AggregateRoot
{
    public long CategoryId { get; private set; }
    public long BranchId { get; private set; }
    public string IFoodCategoryId { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IFoodCategoryMapping() : base(0) { }

    private IFoodCategoryMapping(long categoryId, long branchId, string ifoodCategoryId) : base(0)
    {
        CategoryId = categoryId;
        BranchId = branchId;
        IFoodCategoryId = ifoodCategoryId;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<IFoodCategoryMapping> Create(long categoryId, long branchId, string ifoodCategoryId)
    {
        if (string.IsNullOrWhiteSpace(ifoodCategoryId))
            return Result.Failure<IFoodCategoryMapping>(new Error("IFoodCategoryMapping.EmptyId", "iFood category id is required."));

        return Result.Success(new IFoodCategoryMapping(categoryId, branchId, ifoodCategoryId));
    }
}
