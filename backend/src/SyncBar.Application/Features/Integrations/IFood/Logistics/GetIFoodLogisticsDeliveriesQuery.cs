using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Logistics;

// Entregas de frota própria ainda em andamento (sem código de entrega verificado) de uma filial
// — para a tela "Logística" (fase 7) no frontend.
public sealed record GetIfoodLogisticsDeliveriesQuery(long BranchId) : IQuery<IReadOnlyCollection<IfoodLogisticsDeliveryResponse>>;
