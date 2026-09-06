namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Create
{
    public sealed record CreateKeetaIntegrationOrderEventLogResponse(
        long Id,
        long CompanyId,
        long BranchId,
        string EventId,
        string OrderId,
        string EventType);
}
