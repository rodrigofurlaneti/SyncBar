using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.Create
{
    public sealed record CreateKeetaIntegrationOrderEventLogCommand(
        long CompanyId,
        long BranchId,
        string EventId,
        string OrderId,
        string EventType,
        string? RawPayload,
        DateTime EventCreatedAtUtc) : ICommand<CreateKeetaIntegrationOrderEventLogResponse>;
}
