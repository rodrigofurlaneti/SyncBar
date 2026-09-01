using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

public sealed record IfoodShippingItemInput(string Name, string? ExternalCode, int Quantity, decimal UnitPrice);

// Pede um entregador do Ifood pra um pedido que NÃO veio do Ifood (telefone, WhatsApp, balcão) —
// fase 8. QuoteId vem de GetIfoodShippingQuoteQuery (expira, então precisa ser obtido pouco antes
// de chamar este comando). Itens são um resumo simplificado (nome/quantidade/preço) — sem a
// árvore de opções/complementos do pedido original, ver ressalva em IfoodShippingDelivery.
public sealed record RequestIfoodShippingDriverCommand(
    long BranchId,
    string? OrderReference,
    string CustomerName,
    string CustomerPhoneAreaCode,
    string CustomerPhoneNumber,
    decimal MerchantFee,
    string QuoteId,
    string PostalCode,
    string StreetNumber,
    string StreetName,
    string? Complement,
    string Neighborhood,
    string City,
    string State,
    string? Country,
    string? Reference,
    double? Latitude,
    double? Longitude,
    IReadOnlyCollection<IfoodShippingItemInput> Items) : ICommand<long>;
