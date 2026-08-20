using System.Net.Http.Headers;
using System.Text.Json;
using SyncBar.Application.Abstractions.Integrations.IFood;

namespace SyncBar.Infrastructure.Integrations.IFood;

/// <summary>
/// Cliente HTTP real do módulo Financial do iFood (Fase 4) — só Financial Events + Settlement,
/// conforme escopo acordado (ver comentário completo em IIFoodFinancialClient sobre o porquê os
/// nomes de campo/parâmetros aqui são uma implementação de melhor esforço, não confirmada
/// campo-a-campo contra o texto da doc como os outros clients desta integração).
///
/// Cada registro é parseado individualmente — se um evento/título vier com um formato
/// inesperado, ele é pulado (não derruba o ciclo inteiro) e o payload bruto sempre é guardado
/// em RawPayload pra auditoria/depuração manual, mesmo quando o parse dos campos tipados falha
/// parcialmente.
/// </summary>
internal sealed class IFoodFinancialClient(HttpClient httpClient) : IIFoodFinancialClient
{
    private const string FinancialEventsUrl = "https://merchant-api.ifood.com.br/financial/v3/financial-events";
    private const string SettlementsUrl = "https://merchant-api.ifood.com.br/financial/v3/settlements";

    public async Task<IReadOnlyCollection<IFoodFinancialEventDto>> GetFinancialEventsAsync(
        string accessToken, string merchantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
    {
        var url = $"{FinancialEventsUrl}?merchantId={Uri.EscapeDataString(merchantId)}" +
                   $"&beginCompetenceDate={periodStart:yyyy-MM-dd}&endCompetenceDate={periodEnd:yyyy-MM-dd}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return [];

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var root = ResolveArrayRoot(document.RootElement);
        var events = new List<IFoodFinancialEventDto>();

        foreach (var item in root.EnumerateArray())
        {
            var dto = TryParseFinancialEvent(item);
            if (dto is not null)
                events.Add(dto);
        }

        return events;
    }

    public async Task<IReadOnlyCollection<IFoodSettlementDto>> GetSettlementsAsync(
        string accessToken, string merchantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
    {
        var url = $"{SettlementsUrl}?merchantId={Uri.EscapeDataString(merchantId)}" +
                   $"&beginSettlementDate={periodStart:yyyy-MM-dd}&endSettlementDate={periodEnd:yyyy-MM-dd}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return [];

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var root = ResolveArrayRoot(document.RootElement);
        var settlements = new List<IFoodSettlementDto>();

        foreach (var item in root.EnumerateArray())
        {
            var dto = TryParseSettlement(item);
            if (dto is not null)
                settlements.Add(dto);
        }

        return settlements;
    }

    // Algumas APIs do iFood retornam a lista direto na raiz, outras dentro de { "data": [...] }
    // ou { "financialEvents": [...] } / { "settlements": [...] } — tenta as chaves comuns antes
    // de assumir que a raiz já é o array.
    private static JsonElement ResolveArrayRoot(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
            return root;

        foreach (var key in new[] { "data", "financialEvents", "settlements", "items", "content" })
        {
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(key, out var candidate) && candidate.ValueKind == JsonValueKind.Array)
                return candidate;
        }

        return root; // não é array e nenhuma chave conhecida bateu — EnumerateArray vai lançar, tratado por quem chama via try/catch geral se necessário
    }

    private static IFoodFinancialEventDto? TryParseFinancialEvent(JsonElement item)
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

            return new IFoodFinancialEventDto(
                id, name, description, trigger, amount, hasTransferImpact,
                competence, periodStart, periodEnd, settlementExpected, referenceType, referenceId, raw);
        }
        catch
        {
            return null; // formato inesperado — pulado, não derruba o ciclo (ver comentário na classe)
        }
    }

    private static IFoodSettlementDto? TryParseSettlement(JsonElement item)
    {
        try
        {
            var raw = item.GetRawText();
            var id = GetString(item, "id", "settlementId") ?? Guid.NewGuid().ToString();
            var type = GetString(item, "type") ?? "REPASSE";
            var product = GetString(item, "product");
            var amount = GetDecimal(item, "amount", "value") ?? 0m;
            var status = GetString(item, "status") ?? "UNKNOWN";
            var paymentDate = GetDate(item, "paymentDate", "expectedPaymentDate");

            string? bankCode = null, bankAgency = null, bankAccount = null;
            if (item.TryGetProperty("bankAccount", out var bank) && bank.ValueKind == JsonValueKind.Object)
            {
                bankCode = GetString(bank, "bankCode", "bank");
                bankAgency = GetString(bank, "agency");
                bankAccount = GetString(bank, "account");
            }

            return new IFoodSettlementDto(id, type, product, amount, status, paymentDate, bankCode, bankAgency, bankAccount, raw);
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
