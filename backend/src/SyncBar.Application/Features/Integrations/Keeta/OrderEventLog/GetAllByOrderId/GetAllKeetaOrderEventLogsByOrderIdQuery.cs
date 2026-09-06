using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.OrderEventLog.GetAllByOrderId
{
    public sealed record GetAllKeetaOrderEventLogsByOrderIdQuery(
        string OrderId) : IQuery<IReadOnlyList<KeetaIntegrationOrderEventLogResponse>>;
}
