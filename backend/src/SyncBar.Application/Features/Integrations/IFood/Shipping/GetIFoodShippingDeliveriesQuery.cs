using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

// Entregas via malha do iFood (fase 8) ainda abertas (não canceladas) de uma filial — pra tela
// "Entregas iFood" (envio de pedidos de outros canais).
public sealed record GetIFoodShippingDeliveriesQuery(long BranchId) : IQuery<IReadOnlyCollection<IFoodShippingDeliveryResponse>>;
