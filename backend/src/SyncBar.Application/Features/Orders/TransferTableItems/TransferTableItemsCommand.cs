using MediatR;
using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.TransferTableItems
{
    public sealed record TransferTableItemsCommand(
        long SourceCustomerOrderId,
        long TargetCustomerOrderId,
        IReadOnlyCollection<long> CustomerOrderItemIds,
        long SourceDiningTableId,
        long TargetDiningTableId,
        long ActorEmployeeId) : ICommand<Unit>;
}