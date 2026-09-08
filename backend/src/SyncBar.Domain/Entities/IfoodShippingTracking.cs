using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class IfoodShippingTracking : Entity
{
    private IfoodShippingTracking() : base(0) { }
    public long CompanyId { get; private set; }
    public long BranchId { get; private set; }
    public string OrderId { get; private set; } = null!;
    public DateTime AssignedAtUtc { get; private set; }
    public DateTime NextPollAtUtc { get; private set; }
    public bool IsActive { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public DateTime? ExpectedDelivery { get; private set; }
    public double? DeliveryEtaEndMinutes { get; private set; }
    public double? PickupEtaStartMinutes { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public static IfoodShippingTracking Assigned(long companyId, long branchId, string orderId, DateTime now) =>
        new() { CompanyId = companyId, BranchId = branchId, OrderId = orderId, AssignedAtUtc = now, NextPollAtUtc = now, IsActive = true };

    public void Stop() => IsActive = false;
    public void UpdatePosition(double? latitude, double? longitude, DateTime? expectedDelivery,
        double? deliveryEtaEndMinutes, double? pickupEtaStartMinutes, DateTime now)
    {
        Latitude = latitude;
        Longitude = longitude;
        ExpectedDelivery = expectedDelivery;
        DeliveryEtaEndMinutes = deliveryEtaEndMinutes;
        PickupEtaStartMinutes = pickupEtaStartMinutes;
        UpdatedAtUtc = now;
    }
}
