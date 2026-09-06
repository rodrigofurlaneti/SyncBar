using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.ExistsByEventId
{
    public sealed record ExistsKeetaOrderEventLogByEventIdQuery(
        string EventId) : IQuery<bool>;
}
