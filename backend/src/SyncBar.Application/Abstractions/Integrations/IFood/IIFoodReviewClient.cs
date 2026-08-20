namespace SyncBar.Application.Abstractions.Integrations.IFood;

public sealed record IFoodReviewOrderDto(DateTime? CreatedAt, string? Id, string? ShortId);

public sealed record IFoodReviewListItemDto(
    string Id,
    DateTime? CreatedAt,
    bool Discarded,
    bool Published,
    string? Comment,
    bool Moderated,
    string? ModerationStatus,
    string? Reply,
    double? Score,
    string? SurveyId,
    IFoodReviewOrderDto? Order);

public sealed record IFoodReviewListResultDto(
    long Page, long Size, long Total, long PageCount, IReadOnlyCollection<IFoodReviewListItemDto> Reviews);

public sealed record IFoodReviewAnswerOptionDto(string Id, string? Title);

public sealed record IFoodReviewQuestionDto(string Id, string? Type, string? Title, IReadOnlyCollection<IFoodReviewAnswerOptionDto> Answers);

public sealed record IFoodReviewDetailDto(
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
    string? SurveyId,
    IFoodReviewOrderDto? Order,
    IReadOnlyCollection<IFoodReviewQuestionDto> Questions);

public sealed record IFoodReviewReplyResultDto(DateTime? CreatedAt, string Text, string ReviewId);

public sealed record IFoodReviewSummaryDto(double? Score, long TotalReviewsCount, long ValidReviewsCount);

/// <summary>
/// Abstração para o módulo Review do iFood (Fase 9) — review/v1.0, 4 endpoints, confirmados
/// campo-a-campo contra o texto/response de exemplo da coleção Postman oficial "Merchant API —
/// Review". Implementação real: Infrastructure.Integrations.IFood.IFoodReviewClient.
///
/// Só o v1 foi implementado (v2 tem os mesmos 4 paths, mas devolve "replies[]" em vez de "reply"
/// singular, mais os campos visibility/version — ficou fora do escopo de hoje, ver
/// ifood-integration-status.md).
/// </summary>
public interface IIFoodReviewClient
{
    Task<IFoodReviewListResultDto> GetReviewsAsync(
        string accessToken, string merchantId, int page, int pageSize, bool addCount,
        DateTime? dateFrom, DateTime? dateTo, string sort, string sortBy, CancellationToken cancellationToken = default);

    Task<IFoodReviewDetailDto?> GetReviewByIdAsync(
        string accessToken, string merchantId, string reviewId, CancellationToken cancellationToken = default);

    Task<IFoodReviewReplyResultDto> ReplyReviewAsync(
        string accessToken, string merchantId, string reviewId, string text, CancellationToken cancellationToken = default);

    Task<IFoodReviewSummaryDto?> GetSummaryAsync(
        string accessToken, string merchantId, CancellationToken cancellationToken = default);
}
