using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

// Variante "pedido já existente no iFood" do módulo Shipping — cotação de entrega sob demanda
// pra um IFoodOrder que o lojista decidiu não entregar nem pela logística padrão do iFood nem
// pela frota própria (fase 7). IFoodOrderId é o Id LOCAL (long), mesmo padrão de todo o módulo.
public sealed record GetIFoodOrderShippingQuoteQuery(long IFoodOrderId) : IQuery<IFoodShippingQuoteResponse>;
