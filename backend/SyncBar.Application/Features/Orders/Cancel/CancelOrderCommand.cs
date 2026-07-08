using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.Cancel;

public sealed record CancelOrderCommand(long CustomerOrderId) : ICommand;
