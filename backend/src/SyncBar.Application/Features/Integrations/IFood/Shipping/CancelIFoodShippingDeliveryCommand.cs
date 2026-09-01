using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

public sealed record CancelIfoodShippingDeliveryCommand(long Id, string Reason, int CancellationCode) : ICommand;
