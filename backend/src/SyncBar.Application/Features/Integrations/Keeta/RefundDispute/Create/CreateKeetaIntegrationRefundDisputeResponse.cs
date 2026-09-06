namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.Create
{
    public sealed record CreateKeetaIntegrationRefundDisputeResponse(
        long Id,
        long CompanyId,
        long BranchId,
        string OrderId,
        long AfterSaleOrderId,
        decimal RefundAmount,
        string ResolutionStatus);
}
