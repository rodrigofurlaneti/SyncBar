namespace SyncBar.Application.Abstractions.Integrations.Ifood;

public sealed record IfoodReviewOrderDto(DateTime? CreatedAt, string? Id, string? ShortId);

public sealed record IfoodReviewListItemDto(
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
    IfoodReviewOrderDto? Order);

public sealed record IfoodReviewListResultDto(
    long Page, long Size, long Total, long PageCount, IReadOnlyCollection<IfoodReviewListItemDto> Reviews);

public sealed record IfoodReviewAnswerOptionDto(string Id, string? Title);

public sealed record IfoodReviewQuestionDto(string Id, string? Type, string? Title, IReadOnlyCollection<IfoodReviewAnswerOptionDto> Answers);

public sealed record IfoodReviewDetailDto(
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
    IfoodReviewOrderDto? Order,
    IReadOnlyCollection<IfoodReviewQuestionDto> Questions);

public sealed record IfoodReviewReplyResultDto(DateTime? CreatedAt, string Text, string ReviewId);

public sealed record IfoodReviewSummaryDto(double? Score, long TotalReviewsCount, long ValidReviewsCount);

/// <summary>
/// Abstração para o módulo Review do Ifood (Fase 9) — review/v1.0, 4 endpoints, confirmados
/// campo-a-campo contra o texto/response de exemplo da coleção Postman oficial "Merchant API —
/// Review". Implementação real: Infrastructure.Integrations.Ifood.IfoodReviewClient.
///
/// Só o v1 foi implementado (v2 tem os mesmos 4 paths, mas devolve "replies[]" em vez de "reply"
/// singular, mais os campos visibility/version — ficou fora do escopo de hoje, ver
/// Ifood-integration-status.md).
/// </summary>
public interface IIfoodReviewClient
{
    Task<IfoodReviewListResultDto> GetReviewsAsync(
        string accessToken, string merchantId, int page, int pageSize, bool addCount,
        DateTime? dateFrom, DateTime? dateTo, string sort, string sortBy, CancellationToken cancellationToken = default);

    Task<IfoodReviewDetailDto?> GetReviewByIdAsync(
        string accessToken, string merchantId, string reviewId, CancellationToken cancellationToken = default);

    Task<IfoodReviewReplyResultDto> ReplyReviewAsync(
        string accessToken, string merchantId, string reviewId, string text, CancellationToken cancellationToken = default);

    Task<IfoodReviewSummaryDto?> GetSummaryAsync(
        string accessToken, string merchantId, CancellationToken cancellationToken = default);
}
