using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.UpdateItemStatus;

// IsManager vem das roles do JWT (preenchido no controller) — cancelamento de
// item ja enviado a cozinha e prerrogativa do gerente.
public sealed record UpdateOrderItemStatusCommand(
    long CustomerOrderId,
    long OrderItemId,
    long OrderItemStatusId,
    long? ActorEmployeeId = null,
    bool IsManager = false) : ICommand;
