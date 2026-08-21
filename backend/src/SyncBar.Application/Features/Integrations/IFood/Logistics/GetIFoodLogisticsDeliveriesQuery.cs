using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

// Entregas de frota própria ainda em andamento (sem código de entrega verificado) de uma filial
// — para a tela "Logística" (fase 7) no frontend.
public sealed record GetIFoodLogisticsDeliveriesQuery(long BranchId) : IQuery<IReadOnlyCollection<IFoodLogisticsDeliveryResponse>>;
