using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

// Entregador saiu em direção à loja (origem) para retirar o pedido.
public sealed record MarkIFoodGoingToOriginCommand(long IFoodOrderId) : ICommand;
