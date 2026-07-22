using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.RemoveServiceFee;

public sealed record RemoveServiceFeeCommand(long CustomerOrderId) : ICommand;
