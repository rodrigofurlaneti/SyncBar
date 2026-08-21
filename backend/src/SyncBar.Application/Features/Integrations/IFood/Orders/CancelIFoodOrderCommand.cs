using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

public sealed record CancelIFoodOrderCommand(long IFoodOrderId, string ReasonCode) : ICommand;
