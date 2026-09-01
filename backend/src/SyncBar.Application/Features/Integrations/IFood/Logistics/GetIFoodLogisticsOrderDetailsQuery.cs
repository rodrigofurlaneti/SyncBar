using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Logistics;

// Fase 9c — GET logistics/v1.0/orders/{id} (detalhes da entrega direto no Ifood). A resposta
// oficial não tem schema documentado (ver ressalva em IfoodLogisticsOrderDetailsResult), então o
// JSON completo é devolvido cru em RawPayload — a tela decide o que exibir dele.
public sealed record IfoodLogisticsOrderDetailsResponse(string? RawPayload);

public sealed record GetIfoodLogisticsOrderDetailsQuery(long IfoodOrderId) : IQuery<IfoodLogisticsOrderDetailsResponse>;
