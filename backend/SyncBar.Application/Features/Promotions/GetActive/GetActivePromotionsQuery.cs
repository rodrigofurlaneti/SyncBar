using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Promotions.GetActive;

public sealed record GetActivePromotionsQuery(long BranchId) : IQuery<IReadOnlyCollection<ActivePromotionResponse>>;
