using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SyncBar.Application.Abstractions.Integrations.IFood;

namespace SyncBar.Infrastructure.Integrations.IFood;

/// <summary>
/// Cliente HTTP real do módulo Order do iFood — endpoints e formatos confirmados em 2026-08-19
/// contra a documentação oficial colada pelo usuário (Fundamentos, Guia de implementação,
/// Detalhes de pedido, Eventos de pedido). Cobre o "fluxo essencial": polling de eventos,
/// acknowledgment, detalhes do pedido, confirmar, iniciar preparo, pronto/despachar, cancelar.
///
/// NÃO implementado nesta fase (fora do escopo "essencial", ver ifood-integration-status no
/// projeto claude.ai): rastreamento de entregador, disputas (Handshake), cálculo de
/// preparationStartDateTime pra pedidos agendados, código de retirada/entrega.
/// </summary>
internal sealed class IFoodOrderClient(HttpClient httpClient) : IIFoodOrderClient
{
    private const string BaseUrl = "https://merchant-api.ifood.com.br/order/v1.0";

    public async Task<IReadOnlyCollection<IFoodPollingEvent>> PollEventsAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/orders:polling");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        // 204 No Content é resposta válida (sem eventos novos nesse ciclo).
        if (!response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent)
            return [];

        var payload = await response.Content.ReadFromJsonAsync<PollingResponseDto>(cancellationToken: cancellationToken);
        if (payload?.Events is null)
            return [];

        return payload.Events
            .Select(e => new IFoodPollingEvent(e.Id, e.Code, e.FullCode, e.OrderId, e.CreatedAt))
            .ToList();
    }

    public async Task AcknowledgeEventsAsync(string accessToken, IReadOnlyCollection<string> eventIds, CancellationToken cancellationToken = default)
    {
        if (eventIds.Count == 0) return;

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/orders:acknowledgment")
        {
            Content = JsonContent.Create(new { acknowledgedEventIds = eventIds }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try { await httpClient.SendAsync(request, cancellationToken); }
        catch { /* próximo ciclo de polling tenta de novo — evento não confirmado volta sozinho */ }
    }

    public async Task<IFoodOrderDetailsDto?> GetOrderDetailsAsync(string accessToken, string orderId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/orders/{orderId}");
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
            (dto.Items ?? []).Select(i => new IFoodOrderItemDto(i.ExternalCode, i.Ean, i.Name ?? "Item", i.Quantity, i.UnitPrice)).ToList());
    }

    public Task<IFoodOrderActionResult> ConfirmOrderAsync(string accessToken, string orderId, CancellationToken cancellationToken = default)
        => PostActionAsync($"{BaseUrl}/orders/{orderId}/confirm", accessToken, cancellationToken);

    public Task<IFoodOrderActionResult> StartPreparationAsync(string accessToken, string orderId, CancellationToken cancellationToken = default)
        => PostActionAsync($"{BaseUrl}/orders/{orderId}/startPreparation", accessToken, cancellationToken);

    public Task<IFoodOrderActionResult> ReadyToPickupAsync(string accessToken, string orderId, CancellationToken cancellationToken = default)
        => PostActionAsync($"{BaseUrl}/orders/{orderId}/readyToPickup", accessToken, cancellationToken);

    public Task<IFoodOrderActionResult> DispatchAsync(string accessToken, string orderId, CancellationToken cancellationToken = default)
        => PostActionAsync($"{BaseUrl}/orders/{orderId}/dispatch", accessToken, cancellationToken);

    public async Task<IReadOnlyCollection<IFoodCancellationReasonDto>> GetCancellationReasonsAsync(string accessToken, string orderId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/orders/{orderId}/cancellationReasons");
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
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/orders/{orderId}/requestCancellation")
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
    private sealed record ItemDto(string? ExternalCode, string? Ean, string? Name, decimal Quantity, decimal UnitPrice);
    private sealed record TotalDto(decimal OrderAmount);
    private sealed record DeliveryDto(string? DeliveredBy, DeliveryAddressDto? DeliveryAddress);
    private sealed record DeliveryAddressDto(string? FormattedAddress);
    private sealed record TakeoutDto(string? Mode);
    private sealed record CancellationReasonsResponseDto(List<ReasonDto>? Reasons);
    private sealed record ReasonDto(string Code, string Description);
}
