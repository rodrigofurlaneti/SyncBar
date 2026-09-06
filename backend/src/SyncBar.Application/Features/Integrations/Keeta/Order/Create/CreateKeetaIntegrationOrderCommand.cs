using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.Create
{
    public sealed record CreateKeetaIntegrationOrderCommand(
        long CompanyId,
        long BranchId,
        long CustomerId,
        long CustomerOrderId,
        string KeetaOrderId,
        string DisplayId,
        string InternalMerchantId,
        long KeetaMerchantId,
        string OrderType,
        string DeliveredBy,
        decimal OrderAmount,
        string RawOrderJson,
        DateTime OrderCreatedAtUtc) : ICommand<CreateKeetaIntegrationOrderResponse>;
}
