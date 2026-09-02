using MediatR;
using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Orders.TransferComandaAllItem
{
    public sealed record TransferComandaItemsCommand(
            long SourceCustomerOrderId,
            long TargetCustomerOrderId,
            IReadOnlyCollection<long> CustomerOrderItemIds,
            long SourceComandaId,
            long TargetComandaId,
            long ActorEmployeeId) : ICommand<Unit>;
}