using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetByEventId
{
    public sealed record GetKeetaOrderEventLogByEventIdQuery(
        string EventId) : IQuery<KeetaIntegrationOrderEventLogResponse>;
}
