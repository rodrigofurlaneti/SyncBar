using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.Actions.AcceptRefund
{
    public sealed record AcceptKeetaOrderRefundCommand(long OrderId) : ICommand;
}
