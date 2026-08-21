using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

// Fase 9c — requestDriver do PRÓPRIO módulo Order (order/v1.0/orders/{id}/requestDriver),
// distinto do endpoint homônimo do módulo Shipping (ver ressalva em IIFoodOrderClient).
public sealed record RequestIFoodOrderDriverCommand(long IFoodOrderId) : ICommand;
