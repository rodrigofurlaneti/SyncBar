using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetPendingDisputesByBranch
{
    public sealed record GetPendingKeetaRefundDisputesByBranchQuery(
        long BranchId) : IQuery<IReadOnlyList<KeetaIntegrationRefundDisputeResponse>>;
}
