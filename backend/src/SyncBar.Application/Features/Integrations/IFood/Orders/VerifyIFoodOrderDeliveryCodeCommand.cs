using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

// Fase 9c — verifyDeliveryCode do PRÓPRIO módulo Order (order/v1.0/orders/{id}/verifyDeliveryCode),
// distinto do endpoint homônimo do módulo Logistics (ver IIfoodLogisticsClient.VerifyDeliveryCodeAsync
// e a ressalva em IIfoodOrderClient). Retorna se o código digitado bateu com o esperado pelo Ifood
// (mesmo padrão de ValidateIfoodPickupCodeCommand).
public sealed record VerifyIfoodOrderDeliveryCodeCommand(long IfoodOrderId, string Code) : ICommand<bool>;
