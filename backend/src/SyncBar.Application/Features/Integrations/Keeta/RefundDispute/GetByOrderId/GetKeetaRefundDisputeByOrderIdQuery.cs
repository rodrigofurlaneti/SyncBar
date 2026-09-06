using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetByOrderId
{
    public sealed record GetKeetaRefundDisputeByOrderIdQuery(
        string OrderId) : IQuery<KeetaIntegrationRefundDisputeResponse>;
}
