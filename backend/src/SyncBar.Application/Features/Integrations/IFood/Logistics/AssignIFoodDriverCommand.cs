using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

// Atribui um entregador da frota própria a um pedido iFood — primeiro passo do fluxo de
// logística própria (fase 7). Só faz sentido para pedidos DELIVERY com deliveredBy diferente de
// "IFOOD" (self-delivery) — a tela só oferece esta ação nesse caso, mas o handler não repete a
// validação localmente (o próprio iFood recusa a chamada se o pedido não for elegível).
// IFoodOrderId é o Id LOCAL (long) do IFoodOrder, não a string do iFood — mesmo identificador já
// usado em MarkIFoodOrderReadyCommand/StartIFoodOrderPreparationCommand.
public sealed record AssignIFoodDriverCommand(long IFoodOrderId, string DriverName, string DriverPhone, string DriverVehicleType) : ICommand;
