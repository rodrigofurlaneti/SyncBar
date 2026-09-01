using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

// Fase 9c — requestDriver do PRÓPRIO módulo Order (order/v1.0/orders/{id}/requestDriver),
// distinto do endpoint homônimo do módulo Shipping (ver ressalva em IIfoodOrderClient).
public sealed record RequestIfoodOrderDriverCommand(long IfoodOrderId) : ICommand;
