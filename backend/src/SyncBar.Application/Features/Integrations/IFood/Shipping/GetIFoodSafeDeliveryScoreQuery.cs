using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

public sealed record IfoodSafeDeliveryScoreResponse(string? Score);

public sealed record GetIfoodSafeDeliveryScoreQuery(long Id) : IQuery<IfoodSafeDeliveryScoreResponse>;
