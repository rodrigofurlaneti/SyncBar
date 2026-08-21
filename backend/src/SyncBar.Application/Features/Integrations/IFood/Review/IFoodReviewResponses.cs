namespace SyncBar.Application.Features.Integrations.IFood.Review;

public sealed record IFoodReviewOrderResponse(DateTime? CreatedAt, string? Id, string? ShortId);

public sealed record IFoodReviewListItemResponse(
    string Id,
    DateTime? CreatedAt,
    bool Discarded,
    bool Published,
    string? Comment,
    bool Moderated,
    string? ModerationStatus,
    string? Reply,
    double? Score,
    IFoodReviewOrderResponse? Order);

public sealed record IFoodReviewListResponse(
    long Page, long Size, long Total, long PageCount, IReadOnlyCollection<IFoodReviewListItemResponse> Reviews);

public sealed record IFoodReviewAnswerOptionResponse(string Id, string? Title);

public sealed record IFoodReviewQuestionResponse(string Id, string? Type, string? Title, IReadOnlyCollection<IFoodReviewAnswerOptionResponse> Answers);

public sealed record IFoodReviewDetailResponse(
    string Id,
    DateTime? CreatedAt,
    bool Discarded,
    bool Published,
    string? Comment,
    string? CustomerName,
    bool Moderated,
    string? ModerationStatus,
    string? Reply,
    double? Score,
    IFoodReviewOrderResponse? Order,
    IReadOnlyCollection<IFoodReviewQuestionResponse> Questions);

public sealed record IFoodReviewReplyResponse(DateTime? CreatedAt, string Text, string ReviewId);

public sealed record IFoodReviewSummaryResponse(double? Score, long TotalReviewsCount, long ValidReviewsCount);
