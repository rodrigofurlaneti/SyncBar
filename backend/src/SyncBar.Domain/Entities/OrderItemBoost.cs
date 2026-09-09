using SyncBar.Domain.Primitives;
namespace SyncBar.Domain.Entities;

// Snapshot at order entry: catalog edits must not rewrite preparation instructions or prices.
public sealed class OrderItemBoost : Entity
{
    public long OrderItemId { get; private set; }
    public long ProductBoostId { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal UnitPriceCharged { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }
    private OrderItemBoost() : base(0) { }
    internal static OrderItemBoost Snapshot(ProductBoost source, DateTime now) => new()
    {
        ProductBoostId = source.Id, Name = source.BoostName, UnitPriceCharged = source.IncrementalValue, CreatedAt = now, IsActive = true
    };
    internal OrderItemBoost Copy(DateTime now) => new()
    {
        ProductBoostId = ProductBoostId, Name = Name, UnitPriceCharged = UnitPriceCharged, CreatedAt = now, IsActive = true
    };
}

