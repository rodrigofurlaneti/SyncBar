using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

public sealed record CancelIfoodOrderCommand(long IfoodOrderId, string ReasonCode) : ICommand;
