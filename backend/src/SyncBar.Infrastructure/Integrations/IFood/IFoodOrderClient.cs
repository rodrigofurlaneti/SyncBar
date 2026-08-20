using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
/// NÃO implementado nesta fase (fora do escopo "essencial", ver ifood-integration-status no
/// projeto claude.ai): rastreamento de entregador, disputas (Handshake), cálculo de
/// preparationStartDateTime pra pedidos agendados, código de retirada/entrega, Webhook.
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
}
