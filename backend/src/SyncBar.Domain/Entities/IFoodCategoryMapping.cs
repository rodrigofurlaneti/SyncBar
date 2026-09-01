using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Liga uma Category do SyncBar à categoria correspondente no catálogo do Ifood, por FILIAL —
// o catálogo do Ifood é por merchant (por loja), então a mesma Category (que é por empresa)
// pode virar categorias diferentes em cada merchant. Criada uma vez (POST /categories) e
// reaproveitada em toda sincronização seguinte (PUT /items só referencia o IfoodCategoryId).
public sealed class IfoodCategoryMapping : AggregateRoot
{
    public long CategoryId { get; private set; }
    public long BranchId { get; private set; }
    public string IfoodCategoryId { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; }
    public bool IsActive { get; private set; }

    private IfoodCategoryMapping() : base(0) { }

    private IfoodCategoryMapping(long categoryId, long branchId, string IfoodCategoryId) : base(0)
    {
        CategoryId = categoryId;
        BranchId = branchId;
        IfoodCategoryId = IfoodCategoryId;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<IfoodCategoryMapping> Create(long categoryId, long branchId, string IfoodCategoryId)
    {
        if (string.IsNullOrWhiteSpace(IfoodCategoryId))
            return Result.Failure<IfoodCategoryMapping>(new Error("IfoodCategoryMapping.EmptyId", "Ifood category id is required."));

        return Result.Success(new IfoodCategoryMapping(categoryId, branchId, IfoodCategoryId));
    }
}
