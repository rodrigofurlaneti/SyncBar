using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SyncBar.Application.Abstractions.Integrations.IFood;

namespace SyncBar.Infrastructure.Integrations.IFood;

/// <summary>
/// Cliente HTTP real do módulo Analytics do iFood (Fase 9) — analytics/v1.0, 1 endpoint (Search
/// order metrics KPIs). Ver comentário completo em IIFoodAnalyticsClient sobre o payload padrão
/// usado (o DSL real de filtro/agregação é enorme e não tem os valores válidos documentados
/// campo-a-campo na coleção Postman oficial).
/// </summary>
internal sealed class IFoodAnalyticsClient(HttpClient httpClient) : IIFoodAnalyticsClient
{
    private const string BaseUrl = "https://merchant-api.ifood.com.br/analytics/v1.0/merchants";

    public async Task<IFoodOrderKpisResultDto> GetOrderKpisAsync(
        string accessToken, string merchantId, DateTime periodStart, DateTime periodEnd, int page, int size,
        CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/{Uri.EscapeDataString(merchantId)}/orders/kpis";

        var body = new
        {
            page,
            size,
            filter = new
            {
                referenceDate = new
                {
                    gte = periodStart.ToString("yyyy-MM-dd"),
                    lte = periodEnd.ToString("yyyy-MM-dd"),
                },
            },
            agg = new
            {
                dateIntervals = new[]
                {
                    new { from = periodStart.ToString("yyyy-MM-dd"), to = periodEnd.ToString("yyyy-MM-dd") },
                },
                groupBy = new { fields = new[] { "salesChannel" } },
                metrics = new Dictionary<string, string[]>
                {
                    ["gmv"] = ["sum", "avg"],
                    ["gmvWithoutDelivery"] = ["sum"],
                    ["feesGrossValue"] = ["sum"],
                    ["netDeliveryFee"] = ["sum"],
                },
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new IFoodOrderKpisResultDto(page, []);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        var buckets = new List<string>();
        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in data.EnumerateArray())
                buckets.Add(item.GetRawText());
        }

        var currentPage = root.TryGetProperty("currentPage", out var cp) && cp.ValueKind == JsonValueKind.Number && cp.TryGetInt32(out var cpv)
            ? cpv
            : page;

        return new IFoodOrderKpisResultDto(currentPage, buckets);
    }
}
