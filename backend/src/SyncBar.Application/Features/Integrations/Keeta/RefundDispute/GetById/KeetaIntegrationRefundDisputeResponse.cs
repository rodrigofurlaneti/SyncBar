namespace SyncBar.Application.Features.Integrations.Keeta.RefundDispute.GetById
{
    public sealed record KeetaIntegrationRefundDisputeResponse(
        long Id,
        long CompanyId,
        long BranchId,
        string OrderId,
        long AfterSaleOrderId,
        decimal RefundAmount,
        string Currency,
        string ApplyReason,
        string ResolutionStatus,
        string? DenialReasonCode,
        string? DenialReasonText,
        DateTime ReceivedAtUtc,
        DateTime? ResolvedAtUtc);
}
