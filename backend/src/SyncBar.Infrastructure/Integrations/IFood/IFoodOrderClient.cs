using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SyncBar.Application.Abstractions.Integrations.IFood;

namespace SyncBar.Infrastructure.Integrations.IFood;

/// <summary>
/// Cliente HTTP real dos módulos Order + Events do iFood — endpoints e formatos confirmados em
/// 2026-08-19 contra a documentação oficial colada pelo usuário (Fundamentos, Guia de
/// implementação, Detalhes de pedido, Eventos de pedido, e — Fase 2.1 — a doc completa do
/// módulo Events: Introdução, Eventos de pedido, Polling, Webhook, Presença). Cobre o "fluxo
/// essencial": polling de eventos, acknowledgment, detalhes do pedido, confirmar, iniciar
/// preparo, pronto/despachar, cancelar.
///
/// Fase 2.1: polling/acknowledgment migraram do path documentado na Fase 2
/// (order/v1.0/orders:polling — impreciso) pro path correto e genérico do módulo Events
/// (events/v1.0/events:polling), com filtro de categoria (FOOD/FOOD_SELF_SERVICE — únicas
/// vendidas pelo SyncBar) e o header x-polling-merchants (app centralizado, 1 client_id pra
/// vários merchants — agrupa em lotes de até 100 por chamada em vez de 1 chamada por
/// merchant). As demais ações (detalhes, confirmar, avançar status) continuam no módulo Order
/// (order/v1.0), que não muda nesta fase.
///
/// Fase 6a (extensão): GetOrderDetailsAsync passou a ler item.options (ver IFoodOrderItemOptionDto)
/// — nomes de campo (id/name/quantity/unitPrice) assumidos por analogia com o próprio item
/// (mesma ressalva de confiança já registrada em IIFoodOrderClient).
///
/// Fase 9b: rastreamento (GetOrderTrackingAsync), código de retirada (ValidatePickupCodeAsync) e
/// disputas Handshake accept/reject (AcceptDisputeAsync/RejectDisputeAsync) — endpoints e
/// formatos confirmados em 2026-08-20 contra a doc oficial (Postman collection "Order") colada
/// pelo usuário. Disputas não têm ingestão local de eventos ainda (ver ressalva em
/// IFoodDisputeActionResult) — a equipe informa o disputeId manualmente.
///
/// Fase 9c: fecha os gaps restantes do módulo Order da auditoria de 2026-08-20 — virtual bag
/// (GetVirtualBagAsync), proposta de alternativa em disputa (RequestDisputeAlternativeAsync) e os
/// requestDriver/cancelRequestDriver/verifyDeliveryCode do PRÓPRIO módulo Order (distintos dos
/// homônimos em Shipping/Logistics — ver ressalva em IIFoodOrderClient).
///
/// NÃO implementado nesta fase (fora do escopo "essencial", ver ifood-integration-status no
/// projeto claude.ai): cálculo de preparationStartDateTime pra pedidos agendados, Webhook.
/// </summary>
internal sealed class IFoodOrderClient(HttpClient httpClient) : IIFoodOrderClient
{
    private const string OrderBaseUrl = "https://merchant-api.ifood.com.br/order/v1.0";
    private const string EventsBaseUrl = "https://merchant-api.ifood.com.br/events/v1.0";

    // SyncBar só vende comida — categorias do módulo Grocery (varejo) ficam de fora.
    private const string Categories = "FOOD,FOOD_SELF_SERVICE";

    // x-polling-merchants aceita um conjunto limitado de merchants por chamada; 100 é uma
    // margem segura documentada pra apps centralizados com muitas lojas.
    private const int MerchantBatchSize = 100;

    public async Task<IReadOnlyCollection<IFoodPollingEvent>> PollEventsAsync(
        string accessToken, IReadOnlyCollection<string> merchantIds, CancellationToken cancellationToken = default)
    {
        if (merchantIds.Count == 0)
            return [];

        var allEvents = new List<IFoodPollingEvent>();

        foreach (var batch in merchantIds.Chunk(MerchantBatchSize))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{EventsBaseUrl}/events:polling?categories={Categories}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("x-polling-merchants", string.Join(",", batch));

            using var response = await httpClient.SendAsync(request, cancellationToken);
            // 204 No Content é resposta válida (sem eventos novos nesse ciclo pra esse lote).
            if (!response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent)
                continue;

            var payload = await response.Content.ReadFromJsonAsync<PollingResponseDto>(cancellationToken: cancellationToken);
            if (payload?.Events is null)
                continue;

            allEvents.AddRange(payload.Events.Select(e => new IFoodPollingEvent(e.Id, e.Code, e.FullCode, e.OrderId, e.CreatedAt)));
        }

        return allEvents;
    }

    public async Task AcknowledgeEventsAsync(string accessToken, IReadOnlyCollection<string> eventIds, CancellationToken cancellationToken = default)
    {
        if (eventIds.Count == 0) return;

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{EventsBaseUrl}/events/acknowledgment")
        {
            Content = JsonContent.Create(new { acknowledgedEventIds = eventIds }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try { await httpClient.SendAsync(request, cancellationToken); }
        catch { /* próximo ciclo de polling tenta de novo — evento não confirmado volta sozinho */ }
    }

    public async Task<IFoodOrderDetailsDto?> GetOrderDetailsAsync(string accessToken, string orderId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{OrderBaseUrl}/orders/{orderId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        // 404 é esperado logo após o evento chegar (detalhes ainda não disponíveis) — quem chama
        // decide se tenta de novo no próximo ciclo de polling (não fazemos retry aqui dentro).
        if (!response.IsSuccessStatusCode)
            return null;

        var dto = await response.Content.ReadFromJsonAsync<OrderDetailsResponseDto>(cancellationToken: cancellationToken);
        if (dto is null) return null;

        return new IFoodOrderDetailsDto(
            dto.Id,
            dto.DisplayId,
            dto.OrderType,
            dto.OrderTiming ?? "IMMEDIATE",
            dto.Category ?? "FOOD",
            dto.CreatedAt,
            dto.PreparationStartDateTime,
            dto.Merchant?.Id ?? "",
            dto.Customer?.Name,
            dto.Customer?.Phone?.Number,
            dto.Delivery?.DeliveryAddress?.FormattedAddress,
            dto.Delivery?.DeliveredBy,
            dto.Takeout?.Mode,
            dto.Total?.OrderAmount ?? 0m,
            (dto.Items ?? []).Select(i => new IFoodOrderItemDto(
                i.ExternalCode, i.Ean, i.Name ?? "Item", i.Quantity, i.UnitPrice,
                (i.Options ?? []).Select(o => new IFoodOrderItemOptionDto(o.Id, o.Name, o.Quantity, o.UnitPrice)).ToList()))
                .ToList());
    }

    public Task<IFoodOrderActionResult> ConfirmOrderAsync(string accessToken, string orderId, CancellationToken cancellationToken = default)
        => PostActionAsync($"{OrderBaseUrl}/orders/{orderId}/confirm", accessToken, cancellationToken);

    public Task<IFoodOrderActionResult> StartPreparationAsync(string accessToken, string orderId, CancellationToken cancellationToken = default)
        => PostActionAsync($"{OrderBaseUrl}/orders/{orderId}/startPreparation", accessToken, cancellationToken);

    public Task<IFoodOrderActionResult> ReadyToPickupAsync(string accessToken, string orderId, CancellationToken cancellationToken = default)
        => PostActionAsync($"{OrderBaseUrl}/orders/{orderId}/readyToPickup", accessToken, cancellationToken);

    public Task<IFoodOrderActionResult> DispatchAsync(string accessToken, string orderId, CancellationToken cancellationToken = default)
        => PostActionAsync($"{OrderBaseUrl}/orders/{orderId}/dispatch", accessToken, cancellationToken);

    public async Task<IReadOnlyCollection<IFoodCancellationReasonDto>> GetCancellationReasonsAsync(string accessToken, string orderId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{OrderBaseUrl}/orders/{orderId}/cancellationReasons");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return [];

        var payload = await response.Content.ReadFromJsonAsync<CancellationReasonsResponseDto>(cancellationToken: cancellationToken);
        return (payload?.Reasons ?? [])
            .Select(r => new IFoodCancellationReasonDto(r.Code, r.Description))
            .ToList();
    }

    public async Task<IFoodOrderActionResult> RequestCancellationAsync(string accessToken, string orderId, string reasonCode, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{OrderBaseUrl}/orders/{orderId}/requestCancellation")
        {
            Content = JsonContent.Create(new { reason = reasonCode }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await SendActionAsync(request, cancellationToken);
    }

    public async Task<IFoodOrderTrackingDto?> GetOrderTrackingAsync(string accessToken, string orderId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{OrderBaseUrl}/orders/{orderId}/tracking");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        // 404 é esperado quando ainda não há rastreamento disponível (ex.: entregador ainda não
        // atribuído) — quem chama decide se tenta de novo depois.
        if (!response.IsSuccessStatusCode)
            return null;

        var dto = await response.Content.ReadFromJsonAsync<TrackingResponseDto>(cancellationToken: cancellationToken);
        if (dto is null) return null;

        return new IFoodOrderTrackingDto(dto.Latitude, dto.Longitude, dto.ExpectedDelivery, dto.DeliveryEtaEnd, dto.PickupEtaStart);
    }

    public async Task<IFoodPickupValidationResult> ValidatePickupCodeAsync(string accessToken, string orderId, string code, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{OrderBaseUrl}/orders/{orderId}/validatePickupCode")
        {
            Content = JsonContent.Create(new { code }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IFoodPickupValidationResult(false, false, $"iFood retornou {(int)response.StatusCode}: {Truncate(errorBody)}");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(body))
                return new IFoodPickupValidationResult(true, false, null);

            var dto = System.Text.Json.JsonSerializer.Deserialize<PickupValidationResponseDto>(
                body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return new IFoodPickupValidationResult(true, dto?.Success ?? false, null);
        }
        catch (Exception ex)
        {
            return new IFoodPickupValidationResult(false, false, ex.Message);
        }
    }

    public async Task<IFoodDisputeActionResult> AcceptDisputeAsync(string accessToken, string disputeId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{OrderBaseUrl}/disputes/{disputeId}/accept");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await SendDisputeActionAsync(request, cancellationToken);
    }

    public async Task<IFoodDisputeActionResult> RejectDisputeAsync(string accessToken, string disputeId, string reason, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{OrderBaseUrl}/disputes/{disputeId}/reject")
        {
            Content = JsonContent.Create(new { reason }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await SendDisputeActionAsync(request, cancellationToken);
    }

    private async Task<IFoodDisputeActionResult> SendDisputeActionAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IFoodDisputeActionResult(false, null, $"iFood retornou {(int)response.StatusCode}: {Truncate(errorBody)}");
            }

            var dto = await response.Content.ReadFromJsonAsync<DisputeActionResponseDto>(cancellationToken: cancellationToken);
            return new IFoodDisputeActionResult(true, dto?.Status, null);
        }
        catch (Exception ex)
        {
            return new IFoodDisputeActionResult(false, null, ex.Message);
        }
    }

    public async Task<IFoodDisputeActionResult> RequestDisputeAlternativeAsync(
        string accessToken, string disputeId, string alternativeId, string alternativeType,
        decimal? amount, string? currency, CancellationToken cancellationToken = default)
    {
        object payload = amount is not null
            ? new { type = alternativeType, metadata = new { amount = new { value = amount.Value, currency } } }
            : new { type = alternativeType };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{OrderBaseUrl}/disputes/{disputeId}/alternatives/{alternativeId}")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await SendDisputeActionAsync(request, cancellationToken);
    }

    public async Task<IFoodVirtualBagResult> GetVirtualBagAsync(string accessToken, string orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{OrderBaseUrl}/orders/{orderId}/virtual-bag");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new IFoodVirtualBagResult(false, null, null, null, null, null, null, [], null, null, null, $"iFood retornou {(int)response.StatusCode}: {Truncate(rawBody)}");

            return ParseVirtualBagResponse(rawBody);
        }
        catch (Exception ex)
        {
            return new IFoodVirtualBagResult(false, null, null, null, null, null, null, [], null, null, null, ex.Message);
        }
    }

    // Resposta profundamente aninhada e não confirmada campo-a-campo (ver ressalva na
    // interface) — parsing defensivo com JsonDocument em vez de um record tipado rígido,
    // pra não quebrar a leitura inteira se um sub-objeto vier faltando ou com nome diferente.
    // Extraído de GetVirtualBagAsync (junto com ParseVirtualBagItems/ParseVirtualBagPrices) só
    // pra reduzir a complexidade cognitiva apontada pelo SonarCloud — mesmo comportamento.
    private static IFoodVirtualBagResult ParseVirtualBagResponse(string rawBody)
    {
        using var document = JsonDocument.Parse(rawBody);
        var root = document.RootElement;

        var id = GetJsonString(root, "id");
        var shortCode = GetJsonString(root, "shortCode");
        var status = GetJsonString(root, "status");
        DateTime? createdAt = GetJsonString(root, "createdAt") is { } createdAtStr && DateTime.TryParse(createdAtStr, out var parsedCreatedAt) ? parsedCreatedAt : null;

        var merchantName = GetNestedJsonString(root, "merchant", "name");
        var customerName = GetNestedJsonString(root, "customer", "name");

        var items = new List<IFoodVirtualBagItemDto>();
        string? grossValueAmount = null;
        string? grossValueCurrency = null;
        if (root.TryGetProperty("bag", out var bagEl) && bagEl.ValueKind == JsonValueKind.Object)
        {
            if (bagEl.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
                items.AddRange(ParseVirtualBagItems(itemsEl));

            (grossValueAmount, grossValueCurrency) = ParseVirtualBagGrossValue(bagEl);
        }

        return new IFoodVirtualBagResult(true, id, shortCode, status, createdAt, merchantName, customerName, items, grossValueAmount, grossValueCurrency, rawBody, null);
    }

    private static IEnumerable<IFoodVirtualBagItemDto> ParseVirtualBagItems(JsonElement itemsEl)
    {
        foreach (var item in itemsEl.EnumerateArray())
        {
            var quantity = item.TryGetProperty("quantity", out var qtyEl) && qtyEl.ValueKind == JsonValueKind.Number && qtyEl.TryGetInt32(out var qty) ? qty : 0;
            yield return new IFoodVirtualBagItemDto(GetJsonString(item, "uniqueId"), GetJsonString(item, "name"), quantity, GetJsonString(item, "ean"));
        }
    }

    private static (string? Amount, string? Currency) ParseVirtualBagGrossValue(JsonElement bagEl)
    {
        if (bagEl.TryGetProperty("prices", out var pricesEl) && pricesEl.ValueKind == JsonValueKind.Object &&
            pricesEl.TryGetProperty("grossValue", out var grossEl) && grossEl.ValueKind == JsonValueKind.Object)
            return (GetJsonString(grossEl, "value"), GetJsonString(grossEl, "currency"));

        return (null, null);
    }

    private static string? GetNestedJsonString(JsonElement root, string objectPropertyName, string valuePropertyName)
        => root.TryGetProperty(objectPropertyName, out var nestedEl) && nestedEl.ValueKind == JsonValueKind.Object
            ? GetJsonString(nestedEl, valuePropertyName)
            : null;

    public Task<IFoodOrderActionResult> RequestOrderDriverAsync(string accessToken, string orderId, CancellationToken cancellationToken = default)
        => PostActionAsync($"{OrderBaseUrl}/orders/{orderId}/requestDriver", accessToken, cancellationToken);

    public Task<IFoodOrderActionResult> CancelOrderRequestDriverAsync(string accessToken, string orderId, CancellationToken cancellationToken = default)
        => PostActionAsync($"{OrderBaseUrl}/orders/{orderId}/cancelRequestDriver", accessToken, cancellationToken);

    public async Task<IFoodPickupValidationResult> VerifyOrderDeliveryCodeAsync(string accessToken, string orderId, string code, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{OrderBaseUrl}/orders/{orderId}/verifyDeliveryCode")
        {
            Content = JsonContent.Create(new { code }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IFoodPickupValidationResult(false, false, $"iFood retornou {(int)response.StatusCode}: {Truncate(errorBody)}");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(body))
                return new IFoodPickupValidationResult(true, false, null);

            var dto = JsonSerializer.Deserialize<PickupValidationResponseDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return new IFoodPickupValidationResult(true, dto?.Success ?? false, null);
        }
        catch (Exception ex)
        {
            return new IFoodPickupValidationResult(false, false, ex.Message);
        }
    }

    private static string? GetJsonString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value))
        {
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                _ => null,
            };
        }
        return null;
    }

    private async Task<IFoodOrderActionResult> PostActionAsync(string url, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await SendActionAsync(request, cancellationToken);
    }

    private async Task<IFoodOrderActionResult> SendActionAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return new IFoodOrderActionResult(true, null);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new IFoodOrderActionResult(false, $"iFood retornou {(int)response.StatusCode}: {Truncate(body)}");
        }
        catch (Exception ex)
        {
            return new IFoodOrderActionResult(false, ex.Message);
        }
    }

    private static string Truncate(string value) => value.Length > 300 ? value[..300] + "…" : value;

    // DTOs internos de desserialização — nomes batem com o JSON do iFood; ReadFromJsonAsync sem
    // options explícitas já é case-insensitive por padrão (mesmo padrão usado em IFoodAuthClient).
    private sealed record PollingResponseDto(List<PollingEventDto>? Events);
    private sealed record PollingEventDto(string Id, string Code, string? FullCode, string OrderId, DateTime CreatedAt);
    private sealed record OrderDetailsResponseDto(
        string Id, string? DisplayId, string OrderType, string? OrderTiming, string? Category,
        DateTime CreatedAt, DateTime? PreparationStartDateTime,
        MerchantDto? Merchant, CustomerDto? Customer, List<ItemDto>? Items,
        TotalDto? Total, DeliveryDto? Delivery, TakeoutDto? Takeout);
    private sealed record MerchantDto(string Id, string? Name);
    private sealed record CustomerDto(string? Name, PhoneDto? Phone);
    private sealed record PhoneDto(string? Number);
    // Fase 6a (extensão): Options — lista de complementos escolhidos pra este item (ver ressalva
    // de confiança em IIFoodOrderClient/IFoodOrderItemOptionDto).
    private sealed record ItemDto(string? ExternalCode, string? Ean, string? Name, decimal Quantity, decimal UnitPrice, List<ItemOptionDto>? Options);
    private sealed record ItemOptionDto(string? Id, string? Name, decimal Quantity, decimal UnitPrice);
    private sealed record TotalDto(decimal OrderAmount);
    private sealed record DeliveryDto(string? DeliveredBy, DeliveryAddressDto? DeliveryAddress);
    private sealed record DeliveryAddressDto(string? FormattedAddress);
    private sealed record TakeoutDto(string? Mode);
    private sealed record CancellationReasonsResponseDto(List<ReasonDto>? Reasons);
    private sealed record ReasonDto(string Code, string Description);
    // Fase 9b: tracking traz os campos direto na raiz (deliveryEtaEnd/pickupEtaStart em minutos,
    // trackDate ignorado — não usado na tela).
    private sealed record TrackingResponseDto(double? Latitude, double? Longitude, DateTime? ExpectedDelivery, double? DeliveryEtaEnd, double? PickupEtaStart);
    private sealed record PickupValidationResponseDto(bool Success);
    private sealed record DisputeActionResponseDto(string? Id, string? Status, string? DisputeId, DateTime? CreatedAt);
}
