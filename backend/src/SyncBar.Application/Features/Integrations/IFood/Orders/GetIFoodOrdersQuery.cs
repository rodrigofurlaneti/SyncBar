using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

// Pedidos iFood ainda "abertos" (não concluídos/cancelados) de uma filial — para a tela
// "Pedidos iFood" no frontend.
public sealed record GetIFoodOrdersQuery(long BranchId) : IQuery<IReadOnlyCollection<IFoodOrderResponse>>;
