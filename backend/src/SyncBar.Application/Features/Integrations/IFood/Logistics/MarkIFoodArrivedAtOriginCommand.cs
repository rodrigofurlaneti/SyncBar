using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

// Entregador chegou na loja (origem) para retirar o pedido.
public sealed record MarkIFoodArrivedAtOriginCommand(long IFoodOrderId) : ICommand;
