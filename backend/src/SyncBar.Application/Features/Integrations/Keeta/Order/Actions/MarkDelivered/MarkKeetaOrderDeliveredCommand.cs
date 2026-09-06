using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.MarkDelivered
{
    public sealed record MarkKeetaOrderDeliveredCommand(long OrderId) : ICommand;
}
