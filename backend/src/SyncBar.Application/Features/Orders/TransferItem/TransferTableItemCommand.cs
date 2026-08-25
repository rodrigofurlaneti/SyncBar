using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Orders.TransferItem
{
    public sealed record TransferTableItemCommand(
        long SourceCustomerOrderId,
        long TargetCustomerOrderId,
        long CustomerOrderItemId,
        long SourceDiningTableId,
        long TargetDiningTableId,
        long ActorEmployeeId
    ) : ICommand<long>;
}
