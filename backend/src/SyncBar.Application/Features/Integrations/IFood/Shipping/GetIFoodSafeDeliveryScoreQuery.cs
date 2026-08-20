using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

public sealed record IFoodSafeDeliveryScoreResponse(string? Score);

public sealed record GetIFoodSafeDeliveryScoreQuery(long Id) : IQuery<IFoodSafeDeliveryScoreResponse>;
