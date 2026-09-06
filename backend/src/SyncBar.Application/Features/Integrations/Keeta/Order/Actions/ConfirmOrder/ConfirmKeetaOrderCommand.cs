using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.ConfirmOrder
{
    public sealed record ConfirmKeetaOrderCommand(
        long OrderId,
        string? Reason = null,
        int? PreparationTimeMinutes = null) : ICommand;
}
