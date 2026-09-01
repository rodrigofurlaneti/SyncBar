using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

// Fase 9c — cancelRequestDriver do PRÓPRIO módulo Order (order/v1.0/orders/{id}/cancelRequestDriver),
// distinto do endpoint homônimo do módulo Shipping (ver ressalva em IIfoodOrderClient).
public sealed record CancelIfoodOrderDriverRequestCommand(long IfoodOrderId) : ICommand;
