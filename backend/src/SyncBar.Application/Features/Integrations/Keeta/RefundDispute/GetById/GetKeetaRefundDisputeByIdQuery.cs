using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetById
{
    public sealed record GetKeetaRefundDisputeByIdQuery(
        long Id) : IQuery<KeetaIntegrationRefundDisputeResponse>;
}
