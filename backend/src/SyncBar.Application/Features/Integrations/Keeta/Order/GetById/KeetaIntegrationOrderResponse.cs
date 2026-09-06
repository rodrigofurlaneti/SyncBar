namespace SyncBar.Application.Features.Integrations.Keeta.Order.GetById
{
    public sealed record KeetaIntegrationOrderResponse(
        long Id,
        long CompanyId,
        long BranchId,
        long CustomerId,
        long CustomerOrderId,
        string KeetaOrderId,
        string DisplayId,
        string InternalMerchantId,
        long KeetaMerchantId,
        string Status,
        string OrderType,
        string DeliveredBy,
        decimal OrderAmount,
        string Currency,
        string RawOrderJson,
        DateTime OrderCreatedAtUtc,
        DateTime CreatedAtUtc,
        DateTime? ConfirmedAtUtc,
        DateTime? ReadyForPickupAtUtc,
        DateTime? ConcludedAtUtc);
}
