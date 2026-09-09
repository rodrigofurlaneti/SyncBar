using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class ProductBoost : Entity
{
    public long ProductId { get; private set; }
    public string BoostName { get; private set; } = null!;
    public decimal IncrementalValue { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }
    private ProductBoost() : base(0) { }

    public static Result<ProductBoost> Create(long productId, string name, decimal incrementalValue, int displayOrder)
    {
        if (productId <= 0)
            return Result.Failure<ProductBoost>(new Error("Product.InvalidId", "Product is required."));
        var item = new ProductBoost { ProductId = productId, CreatedAt = DateTime.Now, IsActive = true };
        var result = item.Update(name, incrementalValue, displayOrder);
        item.UpdatedAt = null;
        return result.IsFailure ? Result.Failure<ProductBoost>(result.Error) : Result.Success(item);
    }

    public Result Update(string name, decimal incrementalValue, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 150)
            return Result.Failure(new Error("ProductBoost.InvalidName", "Name must contain 1 to 150 characters."));
        if (displayOrder < 0)
            return Result.Failure(new Error("ProductBoost.InvalidOrder", "Display order cannot be negative."));
        if (incrementalValue < 0 || incrementalValue > 9999999999999999.99m || decimal.Round(incrementalValue, 2) != incrementalValue)
            return Result.Failure(new Error("ProductBoost.InvalidValue", "Value must be nonnegative with at most two decimal places."));
        IncrementalValue = incrementalValue;
        BoostName = name.Trim();
        DisplayOrder = displayOrder;
        UpdatedAt = DateTime.Now;
        return Result.Success();
    }

    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.Now; }
}

