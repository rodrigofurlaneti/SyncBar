using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Orders.TransferComandaItem
{
    public sealed record TransferComandaItemCommand(
        long SourceCustomerOrderId,
        long TargetCustomerOrderId,
        long CustomerOrderItemId,
        long SourceComandaId,
        long TargetComandaId,
        long ActorEmployeeId) : ICommand<long>;
}
