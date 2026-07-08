using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class Category : AggregateRoot
{
    public long CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public int DisplayOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private Category() : base(0) { }

    private Category(long companyId, string name, int displayOrder) : base(0)
    {
        CompanyId = companyId;
        Name = name;
        DisplayOrder = displayOrder;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<Category> Create(long companyId, string name, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Category>(new Error("Category.EmptyName", "Name is required."));
        return Result.Success(new Category(companyId, name, displayOrder));
    }

    public void Touch() => UpdatedAt = DateTime.UtcNow;

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
