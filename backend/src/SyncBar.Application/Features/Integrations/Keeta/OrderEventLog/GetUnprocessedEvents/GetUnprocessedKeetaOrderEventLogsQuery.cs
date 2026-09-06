using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetUnprocessedEvents
{
    public sealed record GetUnprocessedKeetaOrderEventLogsQuery
        : IQuery<IReadOnlyList<KeetaIntegrationOrderEventLogResponse>>;
}
