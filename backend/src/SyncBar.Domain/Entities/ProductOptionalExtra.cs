using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class ProductOptionalExtra : Entity
{
    public long ProductId { get; private set; }
    public string OptionalExtraName { get; private set; } = null!;
    public int DisplayOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }
    private ProductOptionalExtra() : base(0) { }

    public static Result<ProductOptionalExtra> Create(long productId, string name, int displayOrder)
    {
        if (productId <= 0)
            return Result.Failure<ProductOptionalExtra>(new Error("Product.InvalidId", "Product is required."));
        var item = new ProductOptionalExtra { ProductId = productId, CreatedAt = DateTime.Now, IsActive = true };
        var result = item.Update(name, displayOrder);
        item.UpdatedAt = null;
        return result.IsFailure ? Result.Failure<ProductOptionalExtra>(result.Error) : Result.Success(item);
    }

    public Result Update(string name, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 150)
            return Result.Failure(new Error("ProductOptionalExtra.InvalidName", "Name must contain 1 to 150 characters."));
        if (displayOrder < 0)
            return Result.Failure(new Error("ProductOptionalExtra.InvalidOrder", "Display order cannot be negative."));
        OptionalExtraName = name.Trim();
        DisplayOrder = displayOrder;
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.Now; }
}

