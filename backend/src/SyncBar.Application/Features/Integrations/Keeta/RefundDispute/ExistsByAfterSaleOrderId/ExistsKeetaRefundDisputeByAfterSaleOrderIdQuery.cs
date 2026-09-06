using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.ExistsByAfterSaleOrderId
{
    public sealed record ExistsKeetaRefundDisputeByAfterSaleOrderIdQuery(
        long AfterSaleOrderId) : IQuery<bool>;
}
