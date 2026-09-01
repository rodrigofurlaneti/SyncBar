using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

// Fase 9c — Virtual Bag (GET order/v1.0/orders/{id}/virtual-bag). Só os campos de topo mais úteis
// pra tela são expostos aqui (ver ressalva de confiança em IfoodVirtualBagResult); RawPayload
// carrega o JSON completo pra inspeção manual quando necessário.
public sealed record IfoodVirtualBagItemResponse(string? UniqueId, string? Name, int Quantity, string? Ean);

public sealed record IfoodOrderVirtualBagResponse(
    string? Id,
    string? ShortCode,
    string? Status,
    DateTime? CreatedAt,
    string? MerchantName,
    string? CustomerName,
    IReadOnlyCollection<IfoodVirtualBagItemResponse> Items,
    string? GrossValueAmount,
    string? GrossValueCurrency,
    string? RawPayload);

public sealed record GetIfoodOrderVirtualBagQuery(long IfoodOrderId) : IQuery<IfoodOrderVirtualBagResponse>;
