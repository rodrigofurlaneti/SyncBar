using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.RejectRefund
{
    public sealed record RejectKeetaOrderRefundCommand(long OrderId, string Reason, string Code) : ICommand;
}
