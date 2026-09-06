using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.RequestCancellation
{
    public sealed record RequestKeetaOrderCancellationCommand(
        long OrderId,
        string Reason,
        string Code,
        string Mode,
        IReadOnlyList<string>? OutOfStockItems = null,
        IReadOnlyList<string>? InvalidItems = null) : ICommand;
}
