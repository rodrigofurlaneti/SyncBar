using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

public sealed record CancelIFoodOrderShippingDriverCommand(long IFoodOrderId) : ICommand;
