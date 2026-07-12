using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.RaiseComandaLimit;

public sealed record RaiseComandaLimitCommand(long CustomerOrderId, decimal NewLimitAmount) : ICommand;
