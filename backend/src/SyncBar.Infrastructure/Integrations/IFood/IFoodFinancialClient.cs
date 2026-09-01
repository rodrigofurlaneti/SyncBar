using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SyncBar.Application.Abstractions.Integrations.Ifood;

namespace SyncBar.Infrastructure.Integrations.Ifood;

/// <summary>
/// Cliente HTTP real do módulo Financial do Ifood. Cobre os 19 endpoints oficiais:
/// financial/v2.0 (12), financial/v2.1 (1) e financial/v3.0 (6) — ver comentário completo em
/// IIfoodFinancialClient sobre a correção do endpoint "financial-events" (nunca existiu) pra
/// "reconciliation" (real) e sobre por que os relatórios v2.0/v2.1 usam um catálogo genérico.
///
/// Cada registro é parseado individualmente quando o client tenta tipar campos conhecidos — se
/// vier em formato inesperado, é pulado (não derruba o ciclo inteiro) e o payload bruto sempre é
/// guardado em RawPayload/RawItems pra auditoria/depuração manual.
/// </summary>
internal sealed class IfoodFinancialClient(HttpClient httpClient) : IIfoodFinancialClient
{
    private const string BaseUrlV2 = "https://merchant-api.Ifood.com.br/financial/v2.0/merchants";
    private const string BaseUrlV21 = "https://merchant-api.Ifood.com.br/financial/v2.1/merchants";
    private const string BaseUrlV3 = "https://merchant-api.Ifood.com.br/financial/v3.0/merchants";

    public async Task<IReadOnlyCollection<IfoodFinancialEventDto>> GetFinancialEventsAsync(
        string accessToken, string merchantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
    {
        // financial/v3.0/reconciliation é filtrado por "competence" (yyyy-MM), não por
        // intervalo de datas — itera todos os meses distintos cobertos por [periodStart, periodEnd].
        var events = new List<IfoodFinancialEventDto>();
        foreach (var competence in EnumerateCompetences(periodStart, periodEnd))
        {
            var url = $"{BaseUrlV3}/{Uri.EscapeDataString(merchantId)}/reconciliation?competence={competence}";
            var items = await GetRawArrayAsync(url, accessToken, cancellationToken);

            foreach (var item in items)
            {
                var dto = TryParseFinancialEvent(item);
                if (dto is not null)
                    events.Add(dto);
            }
        }

        return events;
    }

    public async Task<IReadOnlyCollection<IfoodSettlementDto>> GetSettlementsAsync(
        string accessToken, string merchantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
    {
        // A coleção Postman oficial não documenta filtros de data pra este endpoint (ao contrário
        // de v2.0/v2.1) — periodStart/periodEnd ficam na assinatura por compatibilidade com quem
        // já chama este método (sync diário), mas não são enviados como query.
        var url = $"{BaseUrlV3}/{Uri.EscapeDataString(merchantId)}/settlements";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return [];

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return ParseSettlements(document.RootElement);
    }

    // Corrigido em 2026-08-20: a resposta real é um objeto único {beginDate, endDate, balance,
    // merchantId, settlements: [{startDateCalculation, endDateCalculation, closingItems: [...]}],
    // consolidatedMerchants} — dois níveis mais fundo do que o catálogo genérico de relatórios
    // (v2.0/v2.1) assume (que espera uma lista plana já na raiz ou numa chave conhecida). Os
    // lançamentos de fato (id/type/amount/...) estão em settlements[].closingItems[], não em
    // settlements[] diretamente — sem este achatamento, cada "período" virava 1 registro
    // falso com id aleatório e amount=0 (nenhum campo batia).
    private static List<IfoodSettlementDto> ParseSettlements(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("settlements", out var periods) && periods.ValueKind == JsonValueKind.Array)
            return ParseSettlementsFromPeriods(periods);

        // Formato inesperado/mudou de novo — tenta o caminho antigo (lista plana) como
        // fallback em vez de simplesmente devolver vazio.
        var fallbackRoot = ResolveArrayRoot(root);
        return fallbackRoot.ValueKind == JsonValueKind.Array
            ? ParseSettlementItems(fallbackRoot)
            : [];
    }

    private static List<IfoodSettlementDto> ParseSettlementsFromPeriods(JsonElement periods)
    {
        var settlements = new List<IfoodSettlementDto>();
        foreach (var period in periods.EnumerateArray())
        {
            if (!period.TryGetProperty("closingItems", out var closingItems) || closingItems.ValueKind != JsonValueKind.Array)
                continue;

            settlements.AddRange(ParseSettlementItems(closingItems));
        }

        return settlements;
    }

    private static List<IfoodSettlementDto> ParseSettlementItems(JsonElement items)
    {
        var settlements = new List<IfoodSettlementDto>();
        foreach (var item in items.EnumerateArray())
        {
            var dto = TryParseSettlement(item);
            if (dto is not null)
                settlements.Add(dto);
        }

        return settlements;
    }

    public async Task<IfoodFinancialReportResultDto> GetAnticipationsAsync(
        string accessToken, string merchantId, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrlV3}/{Uri.EscapeDataString(merchantId)}/anticipations";
        return await GetRawReportAsync(url, accessToken, cancellationToken);
    }

    public async Task<IfoodFinancialReportResultDto> GetSalesV3Async(
        string accessToken, string merchantId, DateTime periodStart, DateTime periodEnd, int page, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrlV3}/{Uri.EscapeDataString(merchantId)}/sales" +
                   $"?beginSalesDate={periodStart:yyyy-MM-dd}&endSalesDate={periodEnd:yyyy-MM-dd}&page={page}";
        return await GetRawReportAsync(url, accessToken, cancellationToken);
    }

    public async Task<IfoodReconciliationOnDemandRequestDto> RequestReconciliationOnDemandAsync(
        string accessToken, string merchantId, string competence, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrlV3}/{Uri.EscapeDataString(merchantId)}/reconciliation/on-demand";
        var payload = JsonSerializer.Serialize(new { competence });

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ifood reconciliation on-demand request failed ({(int)response.StatusCode}): {raw}");

        string requestId;
        try
        {
            using var document = JsonDocument.Parse(raw);
            requestId = GetString(document.RootElement, "requestId", "id") ?? string.Empty;
        }
        catch
        {
            requestId = string.Empty;
        }

        return new IfoodReconciliationOnDemandRequestDto(requestId, raw);
    }

    public async Task<string?> GetReconciliationOnDemandStatusAsync(
        string accessToken, string merchantId, string requestId, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrlV3}/{Uri.EscapeDataString(merchantId)}/reconciliation/on-demand/{Uri.EscapeDataString(requestId)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<IfoodFinancialReportResultDto> GetReportAsync(
        string accessToken, string merchantId, IfoodFinancialReportType reportType,
        string? periodId, DateTime? rangeStart, DateTime? rangeEnd, CancellationToken cancellationToken = default)
    {
        var url = BuildReportUrl(merchantId, reportType, periodId, rangeStart, rangeEnd);
        return await GetRawReportAsync(url, accessToken, cancellationToken);
    }

    // Mapeia cada tipo de relatório pro path e pros nomes reais de query param, confirmados
    // contra a coleção Postman oficial "Merchant API — Financial" (v2.0/v2.1).
    private static string BuildReportUrl(
        string merchantId, IfoodFinancialReportType reportType, string? periodId, DateTime? rangeStart, DateTime? rangeEnd)
    {
        var id = Uri.EscapeDataString(merchantId);
        var start = rangeStart?.ToString("yyyy-MM-dd");
        var end = rangeEnd?.ToString("yyyy-MM-dd");

        return reportType switch
        {
            IfoodFinancialReportType.SalesAdjustments =>
                $"{BaseUrlV2}/{id}/salesAdjustments{Query(("periodId", periodId), ("beginUpdateDate", start), ("endUpdateDate", end))}",
            IfoodFinancialReportType.Payments =>
                $"{BaseUrlV2}/{id}/payments{Query(("periodId", periodId), ("beginExpectedExecutionDate", start), ("endExpectedExecutionDate", end), ("beginConfirmedPaymentDate", start), ("endConfirmedPaymentDate", end))}",
            IfoodFinancialReportType.PaymentDetails =>
                $"{BaseUrlV2}/{id}/paymentDetails{Query(("beginPaymentDate", start), ("endPaymentDate", end))}",
            IfoodFinancialReportType.Occurrences =>
                $"{BaseUrlV2}/{id}/occurrences{Query(("periodId", periodId), ("transactionDateBegin", start), ("transactionDateEnd", end))}",
            IfoodFinancialReportType.MaintenanceFees =>
                $"{BaseUrlV2}/{id}/maintenanceFees{Query(("periodId", periodId), ("transactionDateBegin", start), ("transactionDateEnd", end))}",
            IfoodFinancialReportType.IncomeTaxes =>
                $"{BaseUrlV2}/{id}/incomeTaxes{Query(("periodId", periodId), ("transactionDateBegin", start), ("transactionDateEnd", end))}",
            IfoodFinancialReportType.Periods =>
                $"{BaseUrlV2}/{id}/periods{Query(("competence", periodId))}",
            IfoodFinancialReportType.ChargeCancellations =>
                $"{BaseUrlV2}/{id}/chargeCancellations{Query(("periodId", periodId), ("transactionDateBegin", start), ("transactionDateEnd", end))}",
            IfoodFinancialReportType.Cancellations =>
                $"{BaseUrlV2}/{id}/cancellations{Query(("periodId", periodId), ("beginCancellationDate", start), ("endCancellationDate", end))}",
            IfoodFinancialReportType.ReceivableRecords =>
                $"{BaseUrlV2}/{id}/receivableRecords{Query(("beginReceivableDate", start), ("endReceivableDate", end))}",
            IfoodFinancialReportType.SalesBenefits =>
                $"{BaseUrlV2}/{id}/salesBenefits{Query(("periodId", periodId), ("beginOrderDate", start), ("endOrderDate", end))}",
            IfoodFinancialReportType.AdjustmentsBenefits =>
                $"{BaseUrlV2}/{id}/adjustmentsBenefits{Query(("periodId", periodId), ("beginOrderDate", start), ("endOrderDate", end))}",
            IfoodFinancialReportType.SalesV21 =>
                $"{BaseUrlV21}/{id}/sales{Query(("periodId", periodId), ("beginLastProcessingDate", start), ("endLastProcessingDate", end), ("beginOrderDate", start), ("endOrderDate", end))}",
            IfoodFinancialReportType.AnticipationsV3 =>
                $"{BaseUrlV3}/{id}/anticipations",
            IfoodFinancialReportType.SalesV3 =>
                $"{BaseUrlV3}/{id}/sales{Query(("beginSalesDate", start), ("endSalesDate", end))}",
            _ => throw new ArgumentOutOfRangeException(nameof(reportType), reportType, null),
        };
    }

    private static string Query(params (string Key, string? Value)[] parameters)
    {
        var present = parameters.Where(p => !string.IsNullOrWhiteSpace(p.Value)).ToList();
        if (present.Count == 0)
            return string.Empty;

        return "?" + string.Join("&", present.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}"));
    }

    // Meses distintos (yyyy-MM) cobertos por [periodStart, periodEnd], em ordem.
    private static IEnumerable<string> EnumerateCompetences(DateTime periodStart, DateTime periodEnd)
    {
        var cursor = new DateTime(periodStart.Year, periodStart.Month, 1);
        var last = new DateTime(periodEnd.Year, periodEnd.Month, 1);
        while (cursor <= last)
        {
            yield return cursor.ToString("yyyy-MM");
            cursor = cursor.AddMonths(1);
        }
    }

    private async Task<List<JsonElement>> GetRawArrayAsync(string url, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return [];

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var root = ResolveArrayRoot(document.RootElement);
        var result = new List<JsonElement>();
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                result.Add(item.Clone());
        }

        return result;
    }

    private async Task<IfoodFinancialReportResultDto> GetRawReportAsync(string url, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new IfoodFinancialReportResultDto([]);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var root = ResolveArrayRoot(document.RootElement);
        var items = new List<string>();

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                items.Add(item.GetRawText());
        }
        else
        {
            // Resposta não é uma lista (ex.: objeto único como "periods") — devolve como item único.
            items.Add(root.GetRawText());
        }

        return new IfoodFinancialReportResultDto(items);
    }

    // Algumas APIs do Ifood retornam a lista direto na raiz, outras dentro de { "data": [...] }
    // ou chaves nomeadas — tenta as chaves comuns antes de assumir que a raiz já é o array/objeto.
    private static JsonElement ResolveArrayRoot(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
            return root;

        foreach (var key in new[] { "data", "financialEvents", "reconciliation", "settlements", "anticipations", "sales", "items", "content" })
        {
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(key, out var candidate) && candidate.ValueKind == JsonValueKind.Array)
                return candidate;
        }

        return root;
    }

    private static IfoodFinancialEventDto? TryParseFinancialEvent(JsonElement item)
    {
        try
        {
            var raw = item.GetRawText();
            var id = GetString(item, "id", "eventId") ?? Guid.NewGuid().ToString();
            var name = GetString(item, "name", "eventName") ?? "UNKNOWN";
            var description = GetString(item, "description");
            var trigger = GetString(item, "trigger");
            var amount = GetDecimal(item, "amount", "value") ?? 0m;
            var hasTransferImpact = GetBool(item, "hasTransferImpact") ?? false;
            var competence = GetDate(item, "competenceDate", "competence") ?? DateTime.Today;
            var periodStart = GetDate(item, "periodStartDate", "beginPeriod") ?? competence;
            var periodEnd = GetDate(item, "periodEndDate", "endPeriod") ?? competence;
            var settlementExpected = GetDate(item, "settlementExpectedDate");

            string? referenceType = null;
            string? referenceId = null;
            if (item.TryGetProperty("reference", out var reference) && reference.ValueKind == JsonValueKind.Object)
            {
                referenceType = GetString(reference, "type");
                referenceId = GetString(reference, "id");
            }

            return new IfoodFinancialEventDto(
                id, name, description, trigger, amount, hasTransferImpact,
                competence, periodStart, periodEnd, settlementExpected, referenceType, referenceId, raw);
        }
        catch
        {
            return null; // formato inesperado — pulado, não derruba o ciclo (ver comentário na classe)
        }
    }

    private static IfoodSettlementDto? TryParseSettlement(JsonElement item)
    {
        try
        {
            var raw = item.GetRawText();
            // Campos do closingItem individual (id/type/product/amount/status/paymentDate) —
            // transactionId existe na doc mas ainda não tem lugar no DTO atual, fica só no raw.
            var id = GetString(item, "id", "settlementId") ?? Guid.NewGuid().ToString();
            var type = GetString(item, "type") ?? "REPASSE";
            var product = GetString(item, "product");
            var amount = GetDecimal(item, "amount", "value") ?? 0m;
            var status = GetString(item, "status") ?? "UNKNOWN";
            var paymentDate = GetDate(item, "paymentDate", "expectedPaymentDate");

            // accountDetails é o nome real do wrapper na resposta v3.0 (ver ressalva na classe);
            // bankAccount fica como fallback pro caso de outro formato de relatório reusar este parser.
            string? bankCode = null, bankAgency = null, bankAccount = null;
            if ((item.TryGetProperty("accountDetails", out var bank) || item.TryGetProperty("bankAccount", out bank))
                && bank.ValueKind == JsonValueKind.Object)
            {
                bankCode = GetString(bank, "bankCode", "bank");
                bankAgency = GetString(bank, "agency");
                bankAccount = GetString(bank, "account");
            }

            return new IfoodSettlementDto(id, type, product, amount, status, paymentDate, bankCode, bankAgency, bankAccount, raw);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetString(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
        }
        return null;
    }

    private static decimal? GetDecimal(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (!element.TryGetProperty(name, out var value))
                continue;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
                return number;

            if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out var parsed))
                return parsed;
        }
        return null;
    }

    private static bool? GetBool(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var value) &&
                (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False))
                return value.GetBoolean();
        }
        return null;
    }

    private static DateTime? GetDate(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(value.GetString(), out var parsed))
                return parsed;
        }
        return null;
    }
}
