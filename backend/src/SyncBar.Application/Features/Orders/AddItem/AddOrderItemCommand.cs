using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.AddItem;

public sealed record AddOrderItemCommand(
    long CustomerOrderId,
    long ProductId,
    decimal Quantity,
    string? Notes,
    long? EmployeeId,
    IReadOnlyCollection<OrderItemComplementSelection>? Complements = null,
    IReadOnlyCollection<long>? OptionalExtraIds = null,
    IReadOnlyCollection<long>? BoostIds = null) : ICommand;
