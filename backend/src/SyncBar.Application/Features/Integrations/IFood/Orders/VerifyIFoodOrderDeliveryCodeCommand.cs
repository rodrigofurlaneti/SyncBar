using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

// Fase 9c — verifyDeliveryCode do PRÓPRIO módulo Order (order/v1.0/orders/{id}/verifyDeliveryCode),
// distinto do endpoint homônimo do módulo Logistics (ver IIFoodLogisticsClient.VerifyDeliveryCodeAsync
// e a ressalva em IIFoodOrderClient). Retorna se o código digitado bateu com o esperado pelo iFood
// (mesmo padrão de ValidateIFoodPickupCodeCommand).
public sealed record VerifyIFoodOrderDeliveryCodeCommand(long IFoodOrderId, string Code) : ICommand<bool>;
