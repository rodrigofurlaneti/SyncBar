using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;

namespace SyncBar.Domain.Entities;

public sealed class IfoodOrder : AggregateRoot
{
    public long CustomerOrderId { get; private set; }
    public long BranchId { get; private set; }
    public string IfoodOrderId { get; private set; } = null!;
    public string? DisplayId { get; private set; }
    public string MerchantId { get; private set; } = null!;
    public string IfoodOrderType { get; private set; } = null!;
    public string Status { get; private set; } = null!;
    public string? DeliveredBy { get; private set; }
    public string OrderTiming { get; private set; } = "IMMEDIATE";
    public DateTime? PreparationStartDateTime { get; private set; }
    public DateTime ConfirmDeadlineAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public bool HasUnmappedItems { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private IfoodOrder() : base(0) { }

    private IfoodOrder(
        long customerOrderId, long branchId, string ifoodOrderId, string? displayId, string merchantId,
        string ifoodOrderType, string? deliveredBy, string orderTiming, DateTime? preparationStartDateTime,
        DateTime now, bool hasUnmappedItems) : base(0)
    {
        CustomerOrderId = customerOrderId;
        BranchId = branchId;
        IfoodOrderId = ifoodOrderId;
        DisplayId = displayId;
        MerchantId = merchantId;
        IfoodOrderType = ifoodOrderType;
        DeliveredBy = deliveredBy;
        OrderTiming = string.IsNullOrWhiteSpace(orderTiming) ? "IMMEDIATE" : orderTiming;
        PreparationStartDateTime = preparationStartDateTime;
        Status = IfoodOrderStatuses.Placed;
        ConfirmDeadlineAt = now.AddMinutes(8);
        HasUnmappedItems = hasUnmappedItems;
        IsActive = true;
        CreatedAt = now;
    }

    public static Result<IfoodOrder> Create(
        long customerOrderId, long branchId, string IfoodOrderId, string? displayId, string merchantId,
        string IfoodOrderType, string? deliveredBy, string orderTiming, DateTime? preparationStartDateTime,
        DateTime now, bool hasUnmappedItems)
    {
        if (string.IsNullOrWhiteSpace(IfoodOrderId))
            return Result.Failure<IfoodOrder>(new Error("IfoodOrder.MissingId", "Ifood order id is required."));
        if (string.IsNullOrWhiteSpace(merchantId))
            return Result.Failure<IfoodOrder>(new Error("IfoodOrder.MissingMerchantId", "Merchant id is required."));

        return Result.Success(new IfoodOrder(
            customerOrderId, branchId, IfoodOrderId, displayId, merchantId, IfoodOrderType, deliveredBy,
            orderTiming, preparationStartDateTime, now, hasUnmappedItems));
    }

    public void MarkConfirmed(DateTime now)
    {
        Status = IfoodOrderStatuses.Confirmed;
        ConfirmedAt = now;
        UpdatedAt = now;
    }

    public void MarkCancellationRequested(DateTime now)
    {
        Status = IfoodOrderStatuses.CancellationRequested;
        UpdatedAt = now;
    }

    public void MarkPlaced(DateTime now)
    {
        Status = IfoodOrderStatuses.Placed;
        UpdatedAt = now;
    }

    public void SetStatus(string status, DateTime now)
    {
        Status = status;
        UpdatedAt = now;
    }

    public void Deactivate(DateTime now)
    {
        IsActive = false;
        UpdatedAt = now;
    }
}