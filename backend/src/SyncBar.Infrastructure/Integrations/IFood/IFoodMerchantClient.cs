using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SyncBar.Application.Abstractions.Integrations.IFood;

namespace SyncBar.Infrastructure.Integrations.IFood;

/// <summary>
/// Cliente HTTP real do módulo Merchant do iFood (Fase 5). Endpoints, métodos HTTP e base URL
/// confirmados contra a doc oficial completa — ver comentário detalhado em IIFoodMerchantClient
/// sobre o nível de confiança (endpoints confirmados; formato exato de corpo/resposta é
/// melhor-esforço, parsing defensivo com múltiplos nomes candidatos, igual ao IFoodFinancialClient).
/// </summary>
internal sealed class IFoodMerchantClient(HttpClient httpClient) : IIFoodMerchantClient
{
    private const string BaseUrl = "https://merchant-api.ifood.com.br/merchant/v1.0";

    public async Task<IFoodMerchantStatusResult> GetStatusAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/merchants/{merchantId}/status");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IFoodMerchantStatusResult(false, null, [], $"iFood retornou {(int)response.StatusCode}: {Truncate(body)}");
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = ResolveArrayRoot(document.RootElement, "status", "operations");

            // A doc descreve status por "operação" (ex.: DELIVERY, TAKEOUT) — usa a primeira
            // encontrada como estado geral da loja; se vier um objeto único (sem array), trata
            // ele mesmo como o status.
            var statusElement = root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0
                ? root[0]
                : (document.RootElement.ValueKind == JsonValueKind.Object ? document.RootElement : default);

            var operationState = GetString(statusElement, "state", "status", "operationState");

            var validations = new List<IFoodMerchantValidation>();
            if (statusElement.ValueKind == JsonValueKind.Object &&
                statusElement.TryGetProperty("validations", out var validationsArray) &&
                validationsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var v in validationsArray.EnumerateArray())
                {
                    var id = GetString(v, "id", "code") ?? "UNKNOWN";
                    var state = GetString(v, "state", "status") ?? "UNKNOWN";
                    var message = GetString(v, "message", "description");
                    validations.Add(new IFoodMerchantValidation(id, state, message));
                }
            }

            return new IFoodMerchantStatusResult(true, operationState, validations, null);
        }
        catch (Exception ex)
        {
            return new IFoodMerchantStatusResult(false, null, [], ex.Message);
        }
    }

    public async Task<IFoodInterruptionsResult> GetInterruptionsAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/merchants/{merchantId}/interruptions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IFoodInterruptionsResult(false, [], $"iFood retornou {(int)response.StatusCode}: {Truncate(body)}");
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = ResolveArrayRoot(document.RootElement, "interruptions", "data");

            var interruptions = new List<IFoodInterruption>();
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    var id = GetString(item, "id", "interruptionId");
                    if (id is null)
                        continue;

                    var description = GetString(item, "description", "reason");
                    var start = GetDate(item, "start", "startDate") ?? DateTime.Now;
                    var end = GetDate(item, "end", "endDate") ?? start;
                    interruptions.Add(new IFoodInterruption(id, description, start, end));
                }
            }

            return new IFoodInterruptionsResult(true, interruptions, null);
        }
        catch (Exception ex)
        {
            return new IFoodInterruptionsResult(false, [], ex.Message);
        }
    }

    public async Task<IFoodCreateInterruptionResult> CreateInterruptionAsync(
        string accessToken, string merchantId, string description, DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                description,
                start = start.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                end = end.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/merchants/{merchantId}/interruptions")
            {
                Content = JsonContent.Create(payload),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IFoodCreateInterruptionResult(false, null, $"iFood retornou {(int)response.StatusCode}: {Truncate(errorBody)}");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            string? interruptionId = null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                interruptionId = GetString(doc.RootElement, "id", "interruptionId");
            }
            catch
            {
                // resposta sem corpo JSON válido — segue sem o id, a interrupção ainda foi criada
            }

            return new IFoodCreateInterruptionResult(true, interruptionId, null);
        }
        catch (Exception ex)
        {
            return new IFoodCreateInterruptionResult(false, null, ex.Message);
        }
    }

    public async Task<IFoodMerchantActionResult> DeleteInterruptionAsync(
        string accessToken, string merchantId, string interruptionId, CancellationToken cancellationToken = default)
        => await SendActionAsync(HttpMethod.Delete, $"{BaseUrl}/merchants/{merchantId}/interruptions/{interruptionId}", accessToken, null, null, cancellationToken);

    public async Task<IFoodOpeningHoursResult> GetOpeningHoursAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/merchants/{merchantId}/opening-hours");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IFoodOpeningHoursResult(false, [], $"iFood retornou {(int)response.StatusCode}: {Truncate(body)}");
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = ResolveArrayRoot(document.RootElement, "shifts", "openingHours", "data");

            var shifts = new List<IFoodOpeningHourShift>();
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    var dayOfWeek = ParseDayOfWeek(GetString(item, "dayOfWeek", "day"));
                    var start = ParseTimeOfDay(GetString(item, "start", "startTime"));
                    var durationMinutes = GetInt(item, "duration", "durationMinutes") ?? 0;
                    if (dayOfWeek is null || start is null || durationMinutes <= 0)
                        continue;

                    shifts.Add(new IFoodOpeningHourShift(dayOfWeek.Value, start.Value, durationMinutes));
                }
            }

            return new IFoodOpeningHoursResult(true, shifts, null);
        }
        catch (Exception ex)
        {
            return new IFoodOpeningHoursResult(false, [], ex.Message);
        }
    }

    public async Task<IFoodMerchantActionResult> SetOpeningHoursAsync(
        string accessToken, string merchantId, IReadOnlyCollection<IFoodOpeningHourShift> shifts, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            shifts = shifts.Select(s => new
            {
                dayOfWeek = FormatDayOfWeek(s.DayOfWeek),
                start = s.Start.ToString(@"hh\:mm"),
                duration = s.DurationMinutes,
            }).ToArray(),
        };

        return await SendActionAsync(HttpMethod.Put, $"{BaseUrl}/merchants/{merchantId}/opening-hours", accessToken, payload, null, cancellationToken);
    }

    public async Task<IFoodMerchantActionResult> UpsertPreparationTimeAsync(
        string accessToken, string merchantId, string ifoodCustomerId, int minutes, CancellationToken cancellationToken = default)
    {
        // ⚠️ RISCO CONHECIDO (auditoria de 2026-08-20/21, ver IIFoodMerchantClient): este path
        // (/merchants/{id}/myPreparationTime) NÃO consta na coleção Postman oficial do módulo
        // Merchant — os 9 endpoints reais dessa coleção foram enumerados campo-a-campo e nenhum
        // menciona "Preparation". Mantido como estava por falta de alternativa oficial confirmada
        // (adivinhar um path novo seria pior do que deixar o risco documentado); tratar como não
        // confiável até validação manual em sandbox real.
        var payload = new { preparationTime = minutes };

        // Tenta PUT primeiro (atualizar configuração já existente); se o iFood responder 404
        // ("não configurado ainda"), cai pra POST (criar). Evita ter que rastrear localmente se
        // já existe configuração — ver comentário na interface.
        var putResult = await SendActionAsync(
            HttpMethod.Put, $"{BaseUrl}/merchants/{merchantId}/myPreparationTime", accessToken, payload, ifoodCustomerId, cancellationToken, treat404AsRetryWithPost: true);

        if (putResult.Success || putResult.ErrorMessage?.Contains("__RETRY_POST__") != true)
            return putResult;

        return await SendActionAsync(
            HttpMethod.Post, $"{BaseUrl}/merchants/{merchantId}/myPreparationTime", accessToken, payload, ifoodCustomerId, cancellationToken);
    }

    public async Task<IFoodMerchantActionResult> DeletePreparationTimeAsync(
        string accessToken, string merchantId, string ifoodCustomerId, CancellationToken cancellationToken = default)
        => await SendActionAsync(HttpMethod.Delete, $"{BaseUrl}/merchants/{merchantId}/myPreparationTime", accessToken, null, ifoodCustomerId, cancellationToken);

    public async Task<IFoodMerchantListResult> ListMerchantsAsync(string accessToken, int page = 1, int size = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/merchants?page={page}&size={size}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IFoodMerchantListResult(false, [], $"iFood retornou {(int)response.StatusCode}: {Truncate(body)}");
            }

            var payload = await response.Content.ReadFromJsonAsync<List<MerchantSummaryDto>>(cancellationToken: cancellationToken);
            var merchants = (payload ?? [])
                .Where(m => !string.IsNullOrWhiteSpace(m.Id))
                .Select(m => new IFoodMerchantSummaryDto(m.Id!, m.Name, m.CorporateName))
                .ToList();

            return new IFoodMerchantListResult(true, merchants, null);
        }
        catch (Exception ex)
        {
            return new IFoodMerchantListResult(false, [], ex.Message);
        }
    }

    public async Task<IFoodMerchantDetailsResult> GetMerchantDetailsAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/merchants/{merchantId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IFoodMerchantDetailsResult(false, null, null, null, null, null, null, null, null, $"iFood retornou {(int)response.StatusCode}: {Truncate(body)}");
            }

            var dto = await response.Content.ReadFromJsonAsync<MerchantDetailsDto>(cancellationToken: cancellationToken);
            if (dto is null)
                return new IFoodMerchantDetailsResult(false, null, null, null, null, null, null, null, null, "Resposta vazia do iFood.");

            IFoodMerchantAddressDto? address = dto.Address is null
                ? null
                : new IFoodMerchantAddressDto(
                    dto.Address.Country, dto.Address.State, dto.Address.City, dto.Address.PostalCode, dto.Address.District,
                    dto.Address.Street, dto.Address.Number, dto.Address.Latitude, dto.Address.Longitude);

            return new IFoodMerchantDetailsResult(
                true, dto.Id, dto.Name, dto.CorporateName, dto.Description, dto.Type, dto.Status, dto.CreatedAt, address, null);
        }
        catch (Exception ex)
        {
            return new IFoodMerchantDetailsResult(false, null, null, null, null, null, null, null, null, ex.Message);
        }
    }

    public async Task<IFoodMerchantStatusByOperationResult> GetStatusByOperationAsync(
        string accessToken, string merchantId, string operation, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/merchants/{merchantId}/status/{operation}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IFoodMerchantStatusByOperationResult(false, null, null, false, null, [], $"iFood retornou {(int)response.StatusCode}: {Truncate(body)}");
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            var operationName = GetString(root, "operation");
            var salesChannel = GetString(root, "salesChannel");
            var available = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("available", out var availableEl) &&
                             (availableEl.ValueKind == JsonValueKind.True || availableEl.ValueKind == JsonValueKind.False) && availableEl.GetBoolean();
            var state = GetString(root, "state");

            var validations = new List<IFoodMerchantValidation>();
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("validations", out var validationsArray) &&
                validationsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var v in validationsArray.EnumerateArray())
                {
                    var id = GetString(v, "id", "code") ?? "UNKNOWN";
                    var vState = GetString(v, "state", "status") ?? "UNKNOWN";
                    string? message = null;
                    if (v.TryGetProperty("message", out var messageEl))
                    {
                        message = messageEl.ValueKind == JsonValueKind.Object
                            ? GetString(messageEl, "description", "subtitle", "title")
                            : (messageEl.ValueKind == JsonValueKind.String ? messageEl.GetString() : null);
                    }
                    validations.Add(new IFoodMerchantValidation(id, vState, message));
                }
            }

            return new IFoodMerchantStatusByOperationResult(true, operationName, salesChannel, available, state, validations, null);
        }
        catch (Exception ex)
        {
            return new IFoodMerchantStatusByOperationResult(false, null, null, false, null, [], ex.Message);
        }
    }

    private sealed record MerchantSummaryDto(string? Id, string? Name, string? CorporateName);
    private sealed record MerchantDetailsDto(
        string? Id, string? Name, string? CorporateName, string? Description, string? Type, string? Status,
        DateTime? CreatedAt, MerchantAddressDto? Address);
    private sealed record MerchantAddressDto(
        string? Country, string? State, string? City, string? PostalCode, string? District,
        string? Street, string? Number, double? Latitude, double? Longitude);

    private async Task<IFoodMerchantActionResult> SendActionAsync(
        HttpMethod method, string url, string accessToken, object? payload, string? ifoodCustomerId, CancellationToken cancellationToken,
        bool treat404AsRetryWithPost = false)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            if (payload is not null)
                request.Content = JsonContent.Create(payload);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            if (!string.IsNullOrWhiteSpace(ifoodCustomerId))
                request.Headers.Add("X-iFood-Customer-ID", ifoodCustomerId);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return new IFoodMerchantActionResult(true, null);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (treat404AsRetryWithPost && response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new IFoodMerchantActionResult(false, "__RETRY_POST__");

            return new IFoodMerchantActionResult(false, $"iFood retornou {(int)response.StatusCode}: {Truncate(body)}");
        }
        catch (Exception ex)
        {
            return new IFoodMerchantActionResult(false, ex.Message);
        }
    }

    // Mesma lógica de "chave conhecida ou raiz já é o array" usada no IFoodFinancialClient.
    private static JsonElement ResolveArrayRoot(JsonElement root, params string[] keys)
    {
        if (root.ValueKind == JsonValueKind.Array)
            return root;

        foreach (var key in keys)
        {
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(key, out var candidate) && candidate.ValueKind == JsonValueKind.Array)
                return candidate;
        }

        return root;
    }

    private static int? ParseDayOfWeek(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "SUNDAY" or "SUN" or "DOMINGO" => 0,
        "MONDAY" or "MON" or "SEGUNDA" => 1,
        "TUESDAY" or "TUE" or "TERCA" => 2,
        "WEDNESDAY" or "WED" or "QUARTA" => 3,
        "THURSDAY" or "THU" or "QUINTA" => 4,
        "FRIDAY" or "FRI" or "SEXTA" => 5,
        "SATURDAY" or "SAT" or "SABADO" => 6,
        _ => null,
    };

    private static string FormatDayOfWeek(int dayOfWeek) => dayOfWeek switch
    {
        0 => "SUNDAY",
        1 => "MONDAY",
        2 => "TUESDAY",
        3 => "WEDNESDAY",
        4 => "THURSDAY",
        5 => "FRIDAY",
        6 => "SATURDAY",
        _ => "MONDAY",
    };

    private static TimeSpan? ParseTimeOfDay(string? value)
        => TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static string Truncate(string value) => value.Length > 300 ? value[..300] + "…" : value;

    private static string? GetString(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
        }
        return null;
    }

    private static int? GetInt(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
                return number;
        }
        return null;
    }

    private static DateTime? GetDate(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var name in propertyNames)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(value.GetString(), out var parsed))
                return parsed;
        }
        return null;
    }
}
