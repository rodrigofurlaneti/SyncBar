using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.Reopen;

public sealed record ReopenOrderCommand(long CustomerOrderId) : ICommand;
