using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SyncBar.Application.Abstractions.Integrations.Ifood;

namespace SyncBar.Infrastructure.Integrations.Ifood;

/// <summary>
/// Cliente HTTP real do módulo Review do Ifood (Fase 9) — review/v1.0, 4 endpoints. Nomes de
/// campo confirmados contra o response de exemplo da coleção Postman oficial.
/// </summary>
internal sealed class IfoodReviewClient(HttpClient httpClient) : IIfoodReviewClient
{
    private const string BaseUrl = "https://merchant-api.Ifood.com.br/review/v1.0/merchants";

    public async Task<IfoodReviewListResultDto> GetReviewsAsync(
        string accessToken, string merchantId, int page, int pageSize, bool addCount,
        DateTime? dateFrom, DateTime? dateTo, string sort, string sortBy, CancellationToken cancellationToken = default)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}",
            $"addCount={(addCount ? "true" : "false")}",
            $"sort={Uri.EscapeDataString(sort)}",
            $"sortBy={Uri.EscapeDataString(sortBy)}",
        };
        if (dateFrom is not null)
            query.Add($"dateFrom={dateFrom:O}");
        if (dateTo is not null)
            query.Add($"dateTo={dateTo:O}");

        var url = $"{BaseUrl}/{Uri.EscapeDataString(merchantId)}/reviews?{string.Join("&", query)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new IfoodReviewListResultDto(page, pageSize, 0, 0, []);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        var reviews = new List<IfoodReviewListItemDto>();
        if (root.TryGetProperty("reviews", out var reviewsArray) && reviewsArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in reviewsArray.EnumerateArray())
            {
                var parsed = TryParseListItem(item);
                if (parsed is not null)
                    reviews.Add(parsed);
            }
        }

        return new IfoodReviewListResultDto(
            GetLong(root, "page") ?? page,
            GetLong(root, "size") ?? pageSize,
            GetLong(root, "total") ?? reviews.Count,
            GetLong(root, "pageCount") ?? 0,
            reviews);
    }

    public async Task<IfoodReviewDetailDto?> GetReviewByIdAsync(
        string accessToken, string merchantId, string reviewId, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/{Uri.EscapeDataString(merchantId)}/reviews/{Uri.EscapeDataString(reviewId)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        var questions = new List<IfoodReviewQuestionDto>();
        if (root.TryGetProperty("questions", out var questionsArray) && questionsArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var q in questionsArray.EnumerateArray())
            {
                var answers = new List<IfoodReviewAnswerOptionDto>();
                if (q.TryGetProperty("answers", out var answersArray) && answersArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in answersArray.EnumerateArray())
                        answers.Add(new IfoodReviewAnswerOptionDto(GetString(a, "id") ?? string.Empty, GetString(a, "title")));
                }

                questions.Add(new IfoodReviewQuestionDto(
                    GetString(q, "id") ?? string.Empty, GetString(q, "type"), GetString(q, "title"), answers));
            }
        }

        return new IfoodReviewDetailDto(
            GetString(root, "id") ?? reviewId,
            GetDate(root, "createdAt"),
            GetBool(root, "discarded") ?? false,
            GetBool(root, "published") ?? false,
            GetString(root, "comment"),
            GetString(root, "customerName"),
            GetBool(root, "moderated") ?? false,
            GetString(root, "moderationStatus"),
            GetString(root, "reply"),
            GetDouble(root, "score"),
            GetString(root, "surveyId"),
            TryParseOrder(root),
            questions);
    }

    public async Task<IfoodReviewReplyResultDto> ReplyReviewAsync(
        string accessToken, string merchantId, string reviewId, string text, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/{Uri.EscapeDataString(merchantId)}/reviews/{Uri.EscapeDataString(reviewId)}/answers";
        var payload = JsonSerializer.Serialize(new { text });

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ifood review reply failed ({(int)response.StatusCode}): {raw}");

        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            return new IfoodReviewReplyResultDto(GetDate(root, "createdAt"), GetString(root, "text") ?? text, GetString(root, "reviewId") ?? reviewId);
        }
        catch
        {
            return new IfoodReviewReplyResultDto(null, text, reviewId);
        }
    }

    public async Task<IfoodReviewSummaryDto?> GetSummaryAsync(
        string accessToken, string merchantId, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/{Uri.EscapeDataString(merchantId)}/summary";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        return new IfoodReviewSummaryDto(GetDouble(root, "score"), GetLong(root, "totalReviewsCount") ?? 0, GetLong(root, "validReviewsCount") ?? 0);
    }

    private static IfoodReviewListItemDto? TryParseListItem(JsonElement item)
    {
        try
        {
            return new IfoodReviewListItemDto(
                GetString(item, "id") ?? Guid.NewGuid().ToString(),
                GetDate(item, "createdAt"),
                GetBool(item, "discarded") ?? false,
                GetBool(item, "published") ?? false,
                GetString(item, "comment"),
                GetBool(item, "moderated") ?? false,
                GetString(item, "moderationStatus"),
                GetString(item, "reply"),
                GetDouble(item, "score"),
                GetString(item, "surveyId"),
                TryParseOrder(item));
        }
        catch
        {
            return null;
        }
    }

    private static IfoodReviewOrderDto? TryParseOrder(JsonElement parent)
    {
        if (!parent.TryGetProperty("order", out var order) || order.ValueKind != JsonValueKind.Object)
            return null;

        return new IfoodReviewOrderDto(GetDate(order, "createdAt"), GetString(order, "id"), GetString(order, "shortId"));
    }

    private static string? GetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool? GetBool(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
            ? value.GetBoolean() : null;

    private static double? GetDouble(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var d) ? d : null;

    private static long? GetLong(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var l) ? l : null;

    private static DateTime? GetDate(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String && DateTime.TryParse(value.GetString(), out var parsed)
            ? parsed : null;
}
