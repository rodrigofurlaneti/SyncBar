using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Review;

public sealed record GetIFoodReviewByIdQuery(long BranchId, string ReviewId) : IQuery<IFoodReviewDetailResponse>;
