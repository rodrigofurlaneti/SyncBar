using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Logistics;

// Atribui um entregador da frota própria a um pedido Ifood — primeiro passo do fluxo de
// logística própria (fase 7). Só faz sentido para pedidos DELIVERY com deliveredBy diferente de
// "Ifood" (self-delivery) — a tela só oferece esta ação nesse caso, mas o handler não repete a
// validação localmente (o próprio Ifood recusa a chamada se o pedido não for elegível).
// IfoodOrderId é o Id LOCAL (long) do IfoodOrder, não a string do Ifood — mesmo identificador já
// usado em MarkIfoodOrderReadyCommand/StartIfoodOrderPreparationCommand.
public sealed record AssignIfoodDriverCommand(long IfoodOrderId, string DriverName, string DriverPhone, string DriverVehicleType) : ICommand;
