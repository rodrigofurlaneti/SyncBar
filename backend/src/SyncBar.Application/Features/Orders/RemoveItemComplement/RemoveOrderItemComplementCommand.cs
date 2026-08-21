using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.RemoveItemComplement;

public sealed record RemoveOrderItemComplementCommand(
    long CustomerOrderId,
    long OrderItemId,
    long OrderItemComplementId,
    long? EmployeeId) : ICommand;
