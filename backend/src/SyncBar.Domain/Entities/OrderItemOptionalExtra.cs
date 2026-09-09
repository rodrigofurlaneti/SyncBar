using SyncBar.Domain.Primitives;
namespace SyncBar.Domain.Entities;

// Snapshot at order entry: catalog edits must not rewrite preparation instructions or prices.
public sealed class OrderItemOptionalExtra : Entity
{
    public long OrderItemId { get; private set; }
    public long ProductOptionalExtraId { get; private set; }
    public string Name { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }
    private OrderItemOptionalExtra() : base(0) { }
    internal static OrderItemOptionalExtra Snapshot(ProductOptionalExtra source, DateTime now) => new()
    {
        ProductOptionalExtraId = source.Id, Name = source.OptionalExtraName, CreatedAt = now, IsActive = true
    };
    internal OrderItemOptionalExtra Copy(DateTime now) => new()
    {
        ProductOptionalExtraId = ProductOptionalExtraId, Name = Name, CreatedAt = now, IsActive = true
    };
}

