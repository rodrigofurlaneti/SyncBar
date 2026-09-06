using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetByAfterSaleOrderId
{
    public sealed record GetKeetaRefundDisputeByAfterSaleOrderIdQuery(
        long AfterSaleOrderId) : IQuery<KeetaIntegrationRefundDisputeResponse>;
}
