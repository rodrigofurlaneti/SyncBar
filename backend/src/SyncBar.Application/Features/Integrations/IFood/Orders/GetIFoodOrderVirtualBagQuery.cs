using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

// Fase 9c — Virtual Bag (GET order/v1.0/orders/{id}/virtual-bag). Só os campos de topo mais úteis
// pra tela são expostos aqui (ver ressalva de confiança em IFoodVirtualBagResult); RawPayload
// carrega o JSON completo pra inspeção manual quando necessário.
public sealed record IFoodVirtualBagItemResponse(string? UniqueId, string? Name, int Quantity, string? Ean);

public sealed record IFoodOrderVirtualBagResponse(
    string? Id,
    string? ShortCode,
    string? Status,
    DateTime? CreatedAt,
    string? MerchantName,
    string? CustomerName,
    IReadOnlyCollection<IFoodVirtualBagItemResponse> Items,
    string? GrossValueAmount,
    string? GrossValueCurrency,
    string? RawPayload);

public sealed record GetIFoodOrderVirtualBagQuery(long IFoodOrderId) : IQuery<IFoodOrderVirtualBagResponse>;
