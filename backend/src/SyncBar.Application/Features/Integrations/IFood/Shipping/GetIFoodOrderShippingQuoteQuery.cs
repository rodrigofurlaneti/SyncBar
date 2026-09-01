using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

// Variante "pedido já existente no Ifood" do módulo Shipping — cotação de entrega sob demanda
// pra um IfoodOrder que o lojista decidiu não entregar nem pela logística padrão do Ifood nem
// pela frota própria (fase 7). IfoodOrderId é o Id LOCAL (long), mesmo padrão de todo o módulo.
public sealed record GetIfoodOrderShippingQuoteQuery(long IfoodOrderId) : IQuery<IfoodShippingQuoteResponse>;
