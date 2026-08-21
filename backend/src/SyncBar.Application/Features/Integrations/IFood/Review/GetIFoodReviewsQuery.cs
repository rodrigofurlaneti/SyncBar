using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Review;

public sealed record GetIFoodReviewsQuery(
    long BranchId,
    int Page,
    int PageSize,
    DateTime? DateFrom,
    DateTime? DateTo,
    string Sort,
    string SortBy) : IQuery<IFoodReviewListResponse>;
