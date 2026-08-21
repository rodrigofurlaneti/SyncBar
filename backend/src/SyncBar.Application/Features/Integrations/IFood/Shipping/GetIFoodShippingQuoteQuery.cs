using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

// Cotação de entrega (preço + prazo estimado) pra um endereço, antes de confirmar o pedido de
// motorista — o quoteId devolvido aqui expira (ver ExpirationAt) e é obrigatório no próximo passo
// (RequestIFoodShippingDriverCommand). Não persiste nada — é só uma consulta.
public sealed record GetIFoodShippingQuoteQuery(long BranchId, double Latitude, double Longitude)
    : IQuery<IFoodShippingQuoteResponse>;
