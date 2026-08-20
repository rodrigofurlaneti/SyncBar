using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Review;

public sealed record GetIFoodReviewsSummaryQuery(long BranchId) : IQuery<IFoodReviewSummaryResponse>;
