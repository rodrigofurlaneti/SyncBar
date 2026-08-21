using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

// Fase 9c — GET logistics/v1.0/orders/{id} (detalhes da entrega direto no iFood). A resposta
// oficial não tem schema documentado (ver ressalva em IFoodLogisticsOrderDetailsResult), então o
// JSON completo é devolvido cru em RawPayload — a tela decide o que exibir dele.
public sealed record IFoodLogisticsOrderDetailsResponse(string? RawPayload);

public sealed record GetIFoodLogisticsOrderDetailsQuery(long IFoodOrderId) : IQuery<IFoodLogisticsOrderDetailsResponse>;
