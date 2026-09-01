using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Logistics;

// Entregador saiu da loja com o pedido, a caminho do cliente (destino).
public sealed record DispatchIfoodLogisticsCommand(long IfoodOrderId) : ICommand;
