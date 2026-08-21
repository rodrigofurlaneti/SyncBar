using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

public sealed record RequestIFoodOrderShippingDriverCommand(long IFoodOrderId, string QuoteId) : ICommand;
