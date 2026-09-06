using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetById
{
    public sealed record GetKeetaOrderEventLogByIdQuery(
        long Id) : IQuery<KeetaIntegrationOrderEventLogResponse>;
}
