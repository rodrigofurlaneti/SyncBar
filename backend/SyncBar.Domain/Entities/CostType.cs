using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class CostType : Entity
{
    public string Name { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private CostType() : base(0) { }
}
