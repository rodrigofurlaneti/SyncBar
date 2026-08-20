using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

public sealed record CancelIFoodShippingDeliveryCommand(long Id, string Reason, int CancellationCode) : ICommand;
