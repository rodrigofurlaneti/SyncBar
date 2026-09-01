using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

// Cotação de entrega (preço + prazo estimado) pra um endereço, antes de confirmar o pedido de
// motorista — o quoteId devolvido aqui expira (ver ExpirationAt) e é obrigatório no próximo passo
// (RequestIfoodShippingDriverCommand). Não persiste nada — é só uma consulta.
public sealed record GetIfoodShippingQuoteQuery(long BranchId, double Latitude, double Longitude)
    : IQuery<IfoodShippingQuoteResponse>;
