using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.DispatchOrder
{
    public sealed record DispatchKeetaOrderCommand(
        long OrderId,
        string? TrackingEventType = null,
        string? TrackingEventMessage = null) : ICommand;
}
