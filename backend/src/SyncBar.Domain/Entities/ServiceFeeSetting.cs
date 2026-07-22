using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

// Config por filial: liga/desliga a cobranca da taxa de servico (10%).
// Enabled = false durante eventos em que a taxa nao pode ser cobrada.
public sealed class ServiceFeeSetting : AggregateRoot
{
    public long BranchId { get; private set; }
    public bool Enabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private ServiceFeeSetting() : base(0) { }

    private ServiceFeeSetting(long branchId, bool enabled) : base(0)
    {
        BranchId = branchId;
        Enabled = enabled;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<ServiceFeeSetting> Create(long branchId, bool enabled)
        => Result.Success(new ServiceFeeSetting(branchId, enabled));

    public Result SetEnabled(bool enabled)
    {
        Enabled = enabled;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
