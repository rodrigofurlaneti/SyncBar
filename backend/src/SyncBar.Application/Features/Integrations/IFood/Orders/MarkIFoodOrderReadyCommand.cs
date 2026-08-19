using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

// Marca o pedido como pronto (readyToPickup) — obrigatório para Retirada/DINE_IN, e uma opção
// válida também para Delivery com entregador iFood (ver Guia de implementação, "Pedido pronto
// para entrega"). Entrega com FROTA PRÓPRIA (dispatch) fica fora do fluxo essencial desta fase.
public sealed record MarkIFoodOrderReadyCommand(long IFoodOrderId) : ICommand;
