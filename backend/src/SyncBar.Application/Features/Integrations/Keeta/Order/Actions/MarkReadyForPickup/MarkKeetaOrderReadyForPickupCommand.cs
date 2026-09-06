using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.MarkReadyForPickup
{
    public sealed record MarkKeetaOrderReadyForPickupCommand(long OrderId) : ICommand;
}
