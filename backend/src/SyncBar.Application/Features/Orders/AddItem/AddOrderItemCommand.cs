using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.AddItem;

public sealed record AddOrderItemCommand(
    long CustomerOrderId,
    long ProductId,
    decimal Quantity,
    string? Notes,
    long? EmployeeId) : ICommand;
