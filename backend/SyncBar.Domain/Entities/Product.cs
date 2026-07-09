using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class Product : AggregateRoot
{
    public long CompanyId { get; private set; }
    public long CategoryId { get; private set; }
    public long UnitOfMeasureId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? Barcode { get; private set; }
    public decimal SalePrice { get; private set; }
    public decimal? CostPrice { get; private set; }
    public bool IsStockControlled { get; private set; }
    public int? PreparationTimeMinutes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Product() : base(0) { }

    private Product(long companyId, long categoryId, long unitOfMeasureId, string name, string? description, string? barcode, decimal salePrice, decimal? costPrice, bool isStockControlled, int? preparationTimeMinutes) : base(0)
    {
        CompanyId = companyId;
        CategoryId = categoryId;
        UnitOfMeasureId = unitOfMeasureId;
        Name = name;
        Description = description;
        Barcode = barcode;
        SalePrice = salePrice;
        CostPrice = costPrice;
        IsStockControlled = isStockControlled;
        PreparationTimeMinutes = preparationTimeMinutes;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<Product> Create(long companyId, long categoryId, long unitOfMeasureId, string name, string? description, string? barcode, decimal salePrice, decimal? costPrice, bool isStockControlled, int? preparationTimeMinutes)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Product>(new Error("Product.EmptyName", "Name is required."));
        return Result.Success(new Product(companyId, categoryId, unitOfMeasureId, name, description, barcode, salePrice, costPrice, isStockControlled, preparationTimeMinutes));
    }

    public Result UpdateDetails(long categoryId, long unitOfMeasureId, string name, string? description,
        string? barcode, decimal salePrice, decimal? costPrice, bool isStockControlled, int? preparationTimeMinutes)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(new Error("Product.EmptyName", "Name is required."));
        if (salePrice < 0)
            return Result.Failure(new Error("Product.InvalidSalePrice", "Sale price cannot be negative."));

        CategoryId = categoryId;
        UnitOfMeasureId = unitOfMeasureId;
        Name = name;
        Description = description;
        Barcode = barcode;
        SalePrice = salePrice;
        CostPrice = costPrice;
        IsStockControlled = isStockControlled;
        PreparationTimeMinutes = preparationTimeMinutes;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
