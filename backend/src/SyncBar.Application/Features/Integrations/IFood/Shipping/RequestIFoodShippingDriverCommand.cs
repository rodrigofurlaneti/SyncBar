using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

public sealed record IFoodShippingItemInput(string Name, string? ExternalCode, int Quantity, decimal UnitPrice);

// Pede um entregador do iFood pra um pedido que NÃO veio do iFood (telefone, WhatsApp, balcão) —
// fase 8. QuoteId vem de GetIFoodShippingQuoteQuery (expira, então precisa ser obtido pouco antes
// de chamar este comando). Itens são um resumo simplificado (nome/quantidade/preço) — sem a
// árvore de opções/complementos do pedido original, ver ressalva em IFoodShippingDelivery.
public sealed record RequestIFoodShippingDriverCommand(
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
    IReadOnlyCollection<IFoodShippingItemInput> Items) : ICommand<long>;
