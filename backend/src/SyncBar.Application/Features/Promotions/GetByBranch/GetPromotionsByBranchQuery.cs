using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Promotions.GetByBranch;

public sealed record GetPromotionsByBranchQuery(long BranchId) : IQuery<IReadOnlyCollection<PromotionResponse>>;
