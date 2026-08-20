using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Vínculo (join) entre Product e ComplementGroup — um produto pode ter vários grupos de
// complemento (ex.: hambúrguer tem "Ponto da carne" + "Adicionais"), e o mesmo grupo pode
// ser reaproveitado em vários produtos. Entity standalone com repositório próprio, mesmo
// padrão de UserRole (join AppUser × Role) — não é filha de nenhum aggregate.
public sealed class ProductComplementGroup : Entity
{
    public long ProductId { get; private set; }
    public long ComplementGroupId { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private ProductComplementGroup() : base(0) { }

    private ProductComplementGroup(long productId, long complementGroupId, int displayOrder) : base(0)
    {
        ProductId = productId;
        ComplementGroupId = complementGroupId;
        DisplayOrder = displayOrder;
        IsActive = true;
        CreatedAt = DateTime.Now;
    }

    public static Result<ProductComplementGroup> Create(long productId, long complementGroupId, int displayOrder)
    {
        if (displayOrder < 0)
            return Result.Failure<ProductComplementGroup>(new Error("ProductComplementGroup.InvalidDisplayOrder", "Display order cannot be negative."));

        return Result.Success(new ProductComplementGroup(productId, complementGroupId, displayOrder));
    }

    public void UpdateDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
        UpdatedAt = DateTime.Now;
    }

    public void Touch() => UpdatedAt = DateTime.Now;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.Now;
    }
}
