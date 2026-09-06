namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetById
{
    public sealed record KeetaIntegrationOrderEventLogResponse(
        long Id,
        long CompanyId,
        long BranchId,
        string EventId,
        string OrderId,
        string EventType,
        string? RawPayload,
        bool ProcessedSuccessfully,
        string? ErrorMessage,
        DateTime EventCreatedAtUtc,
        DateTime ReceivedAtUtc);
}
