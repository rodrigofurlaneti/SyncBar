using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

// Pedidos Ifood ainda "abertos" (não concluídos/cancelados) de uma filial — para a tela
// "Pedidos Ifood" no frontend.
public sealed record GetIfoodOrdersQuery(long BranchId) : IQuery<IReadOnlyCollection<IfoodOrderResponse>>;
