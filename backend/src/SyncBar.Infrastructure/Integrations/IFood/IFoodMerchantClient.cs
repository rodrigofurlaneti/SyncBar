using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SyncBar.Application.Abstractions.Integrations.Ifood;

namespace SyncBar.Infrastructure.Integrations.Ifood;

/// <summary>
/// Cliente HTTP real do módulo Merchant do Ifood (Fase 5). Endpoints, métodos HTTP e base URL
/// confirmados contra a doc oficial completa — ver comentário detalhado em IIfoodMerchantClient
/// sobre o nível de confiança (endpoints confirmados; formato exato de corpo/resposta é
/// melhor-esforço, parsing defensivo com múltiplos nomes candidatos, igual ao IfoodFinancialClient).
/// </summary>
internal sealed class IfoodMerchantClient(HttpClient httpClient) : IIfoodMerchantClient
{
    private const string BaseUrl = "https://merchant-api.Ifood.com.br/merchant/v1.0";

    public async Task<IfoodPreparationTimeResult> GetPreparationTimeAsync(string accessToken, string merchantId, string customerId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/merchants/{merchantId}/myPreparationTime");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-iFood-Customer-ID", customerId);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return new(true, null, null);
        if (!response.IsSuccessStatusCode) return new(false, null, $"Falha ao consultar o tempo de preparo no iFood ({(int)response.StatusCode}).");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return new(true, GetInt(document.RootElement, "preparationTime"), null);
    }

    public async Task<IfoodMerchantStatusResult> GetStatusAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/merchants/{merchantId}/status");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IfoodMerchantStatusResult(false, null, false, [], $"Ifood retornou {(int)response.StatusCode}: {Truncate(body)}");
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

            // Fase 13 — a mesma resposta traz um "available: boolean" por operação (confirmado
            // contra a coleção Postman oficial do módulo Merchant), até então descartado por este
            // método. Extraído com o mesmo parsing defensivo já usado em GetStatusByOperationAsync.
            var available = GetBool(statusElement, "available");

            var validations = new List<IfoodMerchantValidation>();
            if (statusElement.ValueKind == JsonValueKind.Object &&
                statusElement.TryGetProperty("validations", out var validationsArray) &&
                validationsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var v in validationsArray.EnumerateArray())
                {
                    var id = GetString(v, "id", "code") ?? "UNKNOWN";
                    var state = GetString(v, "state", "status") ?? "UNKNOWN";
                    var message = ExtractValidationMessage(v) ?? GetString(v, "description");
                    validations.Add(new IfoodMerchantValidation(id, state, message));
                }
            }

            return new IfoodMerchantStatusResult(true, operationState, available, validations, null);
        }
        catch (Exception ex)
        {
            return new IfoodMerchantStatusResult(false, null, false, [], ex.Message);
        }
    }

    public async Task<IfoodInterruptionsResult> GetInterruptionsAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/merchants/{merchantId}/interruptions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IfoodInterruptionsResult(false, [], $"Ifood retornou {(int)response.StatusCode}: {Truncate(body)}");
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = ResolveArrayRoot(document.RootElement, "interruptions", "data");

            var interruptions = new List<IfoodInterruption>();
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
                    interruptions.Add(new IfoodInterruption(id, description, start, end));
                }
            }

            return new IfoodInterruptionsResult(true, interruptions, null);
        }
        catch (Exception ex)
        {
            return new IfoodInterruptionsResult(false, [], ex.Message);
        }
    }

    public async Task<IfoodCreateInterruptionResult> CreateInterruptionAsync(
        string accessToken, string merchantId, string description, DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                description,
                start = start.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                end = end.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
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
                return new IfoodCreateInterruptionResult(false, null, $"Ifood retornou {(int)response.StatusCode}: {Truncate(errorBody)}");
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

            return new IfoodCreateInterruptionResult(true, interruptionId, null);
        }
        catch (Exception ex)
        {
            return new IfoodCreateInterruptionResult(false, null, ex.Message);
        }
    }

    public async Task<IfoodMerchantActionResult> DeleteInterruptionAsync(
        string accessToken, string merchantId, string interruptionId, CancellationToken cancellationToken = default)
        => await SendActionAsync(HttpMethod.Delete, $"{BaseUrl}/merchants/{merchantId}/interruptions/{interruptionId}", accessToken, null, null, cancellationToken);

    public async Task<IfoodOpeningHoursResult> GetOpeningHoursAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/merchants/{merchantId}/opening-hours");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IfoodOpeningHoursResult(false, [], $"Ifood retornou {(int)response.StatusCode}: {Truncate(body)}");
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            // A doc oficial (coleção Postman) devolve um ARRAY de grupos, cada um com um
            // "shifts": [...] aninhado — não um array plano de turnos. Antes desta correção o
            // código tratava a raiz como se já fosse o array de turnos (via ResolveArrayRoot com
            // a chave "shifts", que não confere pq a raiz É um array, então ResolveArrayRoot
            // devolvia ela mesma sem descer no "shifts" de cada item) — cada item iterado era o
            // objeto {shifts:[...]} inteiro, sem propriedade "dayOfWeek" própria, e todo turno
            // era descartado silenciosamente (lista sempre vinha vazia). Trata os dois formatos:
            // raiz já é o array de turnos (fallback) OU raiz é o array de grupos com "shifts".
            var shifts = new List<IfoodOpeningHourShift>();
            foreach (var shiftElement in EnumerateShiftElements(root))
            {
                var dayOfWeek = ParseDayOfWeek(GetString(shiftElement, "dayOfWeek", "day"));
                var start = ParseTimeOfDay(GetString(shiftElement, "start", "startTime"));
                var durationMinutes = GetInt(shiftElement, "duration", "durationMinutes") ?? 0;
                if (dayOfWeek is null || start is null || durationMinutes <= 0)
                    continue;

                shifts.Add(new IfoodOpeningHourShift(dayOfWeek.Value, start.Value, durationMinutes));
            }

            return new IfoodOpeningHoursResult(true, shifts, null);
        }
        catch (Exception ex)
        {
            return new IfoodOpeningHoursResult(false, [], ex.Message);
        }
    }

    public async Task<IfoodMerchantActionResult> SetOpeningHoursAsync(
        string accessToken, string merchantId, IReadOnlyCollection<IfoodOpeningHourShift> shifts, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            // storeId é exigido pelo corpo da requisição na doc oficial (coleção Postman —
            // "Create an opening hours": {"storeId": "<uuid>", "shifts": [...]}) — antes desta
            // correção o payload só enviava "shifts", sem "storeId", o que provavelmente causaria
            // 400 Bad Request ("parâmetro faltando") mesmo com merchantId correto na URL.
            storeId = merchantId,
            shifts = shifts.Select(s => new
            {
                dayOfWeek = FormatDayOfWeek(s.DayOfWeek),
                start = s.Start.ToString(@"hh\:mm\:ss"),
                duration = s.DurationMinutes,
            }).ToArray(),
        };

        return await SendActionAsync(HttpMethod.Put, $"{BaseUrl}/merchants/{merchantId}/opening-hours", accessToken, payload, null, cancellationToken);
    }

    public async Task<IfoodMerchantActionResult> UpsertPreparationTimeAsync(
        string accessToken, string merchantId, string IfoodCustomerId, int minutes, CancellationToken cancellationToken = default)
    {
        // MyPreparationTime receives a raw JSON integer, not an object.
        var payload = minutes;

        // Tenta PUT primeiro (atualizar configuração já existente); se o Ifood responder 404
        // ("não configurado ainda"), cai pra POST (criar). Evita ter que rastrear localmente se
        // já existe configuração — ver comentário na interface.
        var putResult = await SendActionAsync(
            HttpMethod.Put, $"{BaseUrl}/merchants/{merchantId}/myPreparationTime", accessToken, payload, IfoodCustomerId, cancellationToken, treat404AsRetryWithPost: true);

        if (putResult.Success || putResult.ErrorMessage?.Contains("__RETRY_POST__") != true)
            return putResult;

        return await SendActionAsync(
            HttpMethod.Post, $"{BaseUrl}/merchants/{merchantId}/myPreparationTime", accessToken, payload, IfoodCustomerId, cancellationToken);
    }

    public async Task<IfoodMerchantActionResult> DeletePreparationTimeAsync(
        string accessToken, string merchantId, string IfoodCustomerId, CancellationToken cancellationToken = default)
        => await SendActionAsync(HttpMethod.Delete, $"{BaseUrl}/merchants/{merchantId}/myPreparationTime", accessToken, null, IfoodCustomerId, cancellationToken);

    public async Task<IfoodMerchantListResult> ListMerchantsAsync(string accessToken, int page = 1, int size = 100, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/merchants?page={page}&size={size}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IfoodMerchantListResult(false, [], $"Ifood retornou {(int)response.StatusCode}: {Truncate(body)}");
            }

            var payload = await response.Content.ReadFromJsonAsync<List<MerchantSummaryDto>>(cancellationToken: cancellationToken);
            var merchants = (payload ?? [])
                .Where(m => !string.IsNullOrWhiteSpace(m.Id))
                .Select(m => new IfoodMerchantSummaryDto(m.Id!, m.Name, m.CorporateName))
                .ToList();

            return new IfoodMerchantListResult(true, merchants, null);
        }
        catch (Exception ex)
        {
            return new IfoodMerchantListResult(false, [], ex.Message);
        }
    }

    public async Task<IfoodMerchantDetailsResult> GetMerchantDetailsAsync(string accessToken, string merchantId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/merchants/{merchantId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IfoodMerchantDetailsResult(false, null, null, null, null, null, null, null, null, $"Ifood retornou {(int)response.StatusCode}: {Truncate(body)}");
            }

            var dto = await response.Content.ReadFromJsonAsync<MerchantDetailsDto>(cancellationToken: cancellationToken);
            if (dto is null)
                return new IfoodMerchantDetailsResult(false, null, null, null, null, null, null, null, null, "Resposta vazia do Ifood.");

            IfoodMerchantAddressDto? address = dto.Address is null
                ? null
                : new IfoodMerchantAddressDto(
                    dto.Address.Country, dto.Address.State, dto.Address.City, dto.Address.PostalCode, dto.Address.District,
                    dto.Address.Street, dto.Address.Number, dto.Address.Latitude, dto.Address.Longitude);

            return new IfoodMerchantDetailsResult(
                true, dto.Id, dto.Name, dto.CorporateName, dto.Description, dto.Type, dto.Status, dto.CreatedAt, address, null);
        }
        catch (Exception ex)
        {
            return new IfoodMerchantDetailsResult(false, null, null, null, null, null, null, null, null, ex.Message);
        }
    }

    public async Task<IfoodMerchantStatusByOperationResult> GetStatusByOperationAsync(
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
                return new IfoodMerchantStatusByOperationResult(false, null, null, false, null, [], $"Ifood retornou {(int)response.StatusCode}: {Truncate(body)}");
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            var operationName = GetString(root, "operation");
            var salesChannel = GetString(root, "salesChannel");
            var available = GetBool(root, "available");
            var state = GetString(root, "state");
            var validations = ParseValidations(root);

            return new IfoodMerchantStatusByOperationResult(true, operationName, salesChannel, available, state, validations, null);
        }
        catch (Exception ex)
        {
            return new IfoodMerchantStatusByOperationResult(false, null, null, false, null, [], ex.Message);
        }
    }

    private sealed record MerchantSummaryDto(string? Id, string? Name, string? CorporateName);
    private sealed record MerchantDetailsDto(
        string? Id, string? Name, string? CorporateName, string? Description, string? Type, string? Status,
        DateTime? CreatedAt, MerchantAddressDto? Address);
    private sealed record MerchantAddressDto(
        string? Country, string? State, string? City, string? PostalCode, string? District,
        string? Street, string? Number, double? Latitude, double? Longitude);

    private async Task<IfoodMerchantActionResult> SendActionAsync(
        HttpMethod method, string url, string accessToken, object? payload, string? IfoodCustomerId, CancellationToken cancellationToken,
        bool treat404AsRetryWithPost = false)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            if (payload is not null)
                request.Content = JsonContent.Create(payload);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            if (!string.IsNullOrWhiteSpace(IfoodCustomerId))
                request.Headers.Add("X-Ifood-Customer-ID", IfoodCustomerId);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return new IfoodMerchantActionResult(true, null);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (treat404AsRetryWithPost && response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new IfoodMerchantActionResult(false, "__RETRY_POST__");

            return new IfoodMerchantActionResult(false, $"Ifood retornou {(int)response.StatusCode}: {Truncate(body)}");
        }
        catch (Exception ex)
        {
            return new IfoodMerchantActionResult(false, ex.Message);
        }
    }

    // Mesma lógica de "chave conhecida ou raiz já é o array" usada no IfoodFinancialClient.
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

    // Ver comentário em GetOpeningHoursAsync: a doc oficial (coleção Postman) devolve um ARRAY de
    // grupos {shifts:[...]}. Aceita também um único objeto {shifts:[...]} na raiz (sem o array
    // externo) e um array plano de turnos como fallback defensivo (mesmo padrão de "múltiplos
    // formatos candidatos" usado no resto do client), cobrindo variações já vistas em resposta
    // real/mocks de teste.
    private static IEnumerable<JsonElement> EnumerateShiftElements(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("shifts", out var singleGroupShifts) && singleGroupShifts.ValueKind == JsonValueKind.Array)
            {
                foreach (var shift in singleGroupShifts.EnumerateArray())
                    yield return shift;
            }
            yield break;
        }

        if (root.ValueKind != JsonValueKind.Array)
            yield break;

        foreach (var group in root.EnumerateArray())
        {
            if (group.ValueKind == JsonValueKind.Object &&
                group.TryGetProperty("shifts", out var nestedShifts) &&
                nestedShifts.ValueKind == JsonValueKind.Array)
            {
                foreach (var shift in nestedShifts.EnumerateArray())
                    yield return shift;
            }
            else if (group.ValueKind == JsonValueKind.Object && group.TryGetProperty("dayOfWeek", out _))
            {
                // Formato plano (fallback): o próprio item já é um turno.
                yield return group;
            }
        }
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

    private static bool GetBool(JsonElement element, string propertyName)
        => element.ValueKind == JsonValueKind.Object &&
           element.TryGetProperty(propertyName, out var value) &&
           (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False) &&
           value.GetBoolean();

    // Extraído de GetStatusByOperationAsync (reduz Cognitive Complexity — issue Sonar) e reaproveitado
    // por GetStatusAsync: parsing defensivo do array "validations" presente em ambas as respostas.
    private static List<IfoodMerchantValidation> ParseValidations(JsonElement root)
    {
        var validations = new List<IfoodMerchantValidation>();
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("validations", out var validationsArray) ||
            validationsArray.ValueKind != JsonValueKind.Array)
            return validations;

        foreach (var v in validationsArray.EnumerateArray())
            validations.Add(ParseValidation(v));

        return validations;
    }

    private static IfoodMerchantValidation ParseValidation(JsonElement v)
    {
        var id = GetString(v, "id", "code") ?? "UNKNOWN";
        var state = GetString(v, "state", "status") ?? "UNKNOWN";
        var message = ExtractValidationMessage(v);
        return new IfoodMerchantValidation(id, state, message);
    }

    private static string? ExtractValidationMessage(JsonElement v)
    {
        if (!v.TryGetProperty("message", out var messageEl))
            return null;

        return messageEl.ValueKind switch
        {
            JsonValueKind.Object => GetString(messageEl, "description", "subtitle", "title"),
            JsonValueKind.String => messageEl.GetString(),
            _ => null,
        };
    }
}
