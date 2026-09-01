using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

public sealed record AcceptDeliveryAddressChangeCommand(long IfoodOrderId) : ICommand;
