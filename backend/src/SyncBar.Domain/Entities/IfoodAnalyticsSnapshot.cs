using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class IfoodAnalyticsSnapshot : Entity
{
    private IfoodAnalyticsSnapshot() : base(0) { }
    public long BranchId { get; private set; }
    public string MerchantId { get; private set; } = "";
    public DateTime ReferenceDate { get; private set; }
    public string AggregatesJson { get; private set; } = "[]";
    public DateTime RefreshedAtUtc { get; private set; }

    public static IfoodAnalyticsSnapshot Create(long branchId, string merchantId, DateTime date)
        => new() { BranchId = branchId, MerchantId = merchantId, ReferenceDate = date.Date };

    public void Replace(string aggregatesJson, DateTime nowUtc)
    {
        AggregatesJson = aggregatesJson;
        RefreshedAtUtc = nowUtc;
    }
}
