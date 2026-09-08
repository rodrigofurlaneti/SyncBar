using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.MarkReady;

public sealed record MarkOrderReadyCommand(long CustomerOrderId) : ICommand;
