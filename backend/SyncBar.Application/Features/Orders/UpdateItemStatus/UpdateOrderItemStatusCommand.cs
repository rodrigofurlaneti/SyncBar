using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.UpdateItemStatus;

public sealed record UpdateOrderItemStatusCommand(
    long CustomerOrderId,
    long OrderItemId,
    long OrderItemStatusId) : ICommand;
