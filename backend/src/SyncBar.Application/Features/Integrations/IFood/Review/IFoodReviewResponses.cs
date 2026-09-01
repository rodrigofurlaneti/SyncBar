namespace SyncBar.Application.Features.Integrations.Ifood.Review;

public sealed record IfoodReviewOrderResponse(DateTime? CreatedAt, string? Id, string? ShortId);

public sealed record IfoodReviewListItemResponse(
    string Id,
    DateTime? CreatedAt,
    bool Discarded,
    bool Published,
    string? Comment,
    bool Moderated,
    string? ModerationStatus,
    string? Reply,
    double? Score,
    IfoodReviewOrderResponse? Order);

public sealed record IfoodReviewListResponse(
    long Page, long Size, long Total, long PageCount, IReadOnlyCollection<IfoodReviewListItemResponse> Reviews);

public sealed record IfoodReviewAnswerOptionResponse(string Id, string? Title);

public sealed record IfoodReviewQuestionResponse(string Id, string? Type, string? Title, IReadOnlyCollection<IfoodReviewAnswerOptionResponse> Answers);

public sealed record IfoodReviewDetailResponse(
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
    IfoodReviewOrderResponse? Order,
    IReadOnlyCollection<IfoodReviewQuestionResponse> Questions);

public sealed record IfoodReviewReplyResponse(DateTime? CreatedAt, string Text, string ReviewId);

public sealed record IfoodReviewSummaryResponse(double? Score, long TotalReviewsCount, long ValidReviewsCount);
