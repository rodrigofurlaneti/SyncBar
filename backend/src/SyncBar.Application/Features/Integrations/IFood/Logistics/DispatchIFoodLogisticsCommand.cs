using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

// Entregador saiu da loja com o pedido, a caminho do cliente (destino).
public sealed record DispatchIFoodLogisticsCommand(long IFoodOrderId) : ICommand;
