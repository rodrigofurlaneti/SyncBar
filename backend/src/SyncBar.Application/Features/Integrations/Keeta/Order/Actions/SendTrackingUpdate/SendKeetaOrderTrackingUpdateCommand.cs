using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.SendTrackingUpdate
{
    public sealed record SendKeetaOrderTrackingUpdateCommand(
        long OrderId,
        string TrackingEventType,
        string? TrackingEventMessage = null) : ICommand;
}
