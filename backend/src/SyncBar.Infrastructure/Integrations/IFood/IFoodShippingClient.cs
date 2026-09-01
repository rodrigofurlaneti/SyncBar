using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SyncBar.Application.Abstractions.Integrations.Ifood;

namespace SyncBar.Infrastructure.Integrations.Ifood;

/// <summary>
/// Cliente HTTP real do módulo Shipping do Ifood (fase 8, entrega via malha do Ifood pra pedidos
/// de outros canais) — endpoints e formatos confirmados em 2026-08-20 contra a documentação
/// oficial (Postman collection "Shipping") colada pelo usuário. Base URL própria
/// (shipping/v1.0) — diferente de order/v1.0 e logistics/v1.0, mesmo padrão de "nome de endpoint
/// parecido, módulo diferente" já visto entre Order.dispatch e Logistics.dispatch (fase 7).
/// </summary>
internal sealed class IfoodShippingClient(HttpClient httpClient) : IIfoodShippingClient
{
    private const string BaseUrl = "https://merchant-api.Ifood.com.br/shipping/v1.0";

    public async Task<IfoodShippingQuoteResult> GetDeliveryAvailabilitiesAsync(
        string accessToken, string merchantId, double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/merchants/{merchantId}/deliveryAvailabilities" +
                   $"?Latitude={latitude.ToString(CultureInfo.InvariantCulture)}&Longitude={longitude.ToString(CultureInfo.InvariantCulture)}";
        return await GetQuoteAsync(url, accessToken, cancellationToken);
    }

    public async Task<IfoodShippingQuoteResult> GetDeliveryAvailabilitiesForOrderAsync(
        string accessToken, string IfoodOrderId, CancellationToken cancellationToken = default)
        => await GetQuoteAsync($"{BaseUrl}/orders/{IfoodOrderId}/deliveryAvailabilities", accessToken, cancellationToken);

    private async Task<IfoodShippingQuoteResult> GetQuoteAsync(string url, string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IfoodShippingQuoteResult(false, $"Ifood retornou {(int)response.StatusCode}: {Truncate(body)}",
                    null, 0, 0, 0, 0, 0, 0, null);
            }

            var dto = await response.Content.ReadFromJsonAsync<QuoteResponseDto>(cancellationToken: cancellationToken);
            if (dto is null)
                return new IfoodShippingQuoteResult(false, "Resposta vazia do Ifood.", null, 0, 0, 0, 0, 0, 0, null);

            return new IfoodShippingQuoteResult(
                true, null, dto.Id,
                dto.Quote?.GrossValue ?? 0, dto.Quote?.Discount ?? 0, dto.Quote?.NetValue ?? 0,
                dto.DeliveryTime?.Min ?? 0, dto.DeliveryTime?.Max ?? 0, dto.Distance ?? 0, dto.ExpirationAt);
        }
        catch (Exception ex)
        {
            return new IfoodShippingQuoteResult(false, ex.Message, null, 0, 0, 0, 0, 0, 0, null);
        }
    }

    public async Task<IfoodShippingRequestDriverResult> RequestDriverAsync(
        string accessToken, string merchantId, IfoodShippingRequestDriverPayload payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new
            {
                customer = new
                {
                    name = payload.CustomerName,
                    phone = new { countryCode = "55", areaCode = payload.CustomerPhoneAreaCode, number = payload.CustomerPhoneNumber },
                },
                delivery = new
                {
                    merchantFee = payload.MerchantFee,
                    quoteId = payload.QuoteId,
                    deliveryAddress = new
                    {
                        postalCode = payload.PostalCode,
                        streetNumber = payload.StreetNumber,
                        streetName = payload.StreetName,
                        complement = payload.Complement,
                        neighborhood = payload.Neighborhood,
                        city = payload.City,
                        state = payload.State,
                        country = payload.Country,
                        reference = payload.Reference,
                        coordinates = payload.Latitude.HasValue && payload.Longitude.HasValue
                            ? new { latitude = payload.Latitude.Value, longitude = payload.Longitude.Value }
                            : null,
                    },
                },
                items = payload.Items.Select(i => new
                {
                    name = i.Name,
                    externalCode = i.ExternalCode,
                    quantity = i.Quantity,
                    unitPrice = i.UnitPrice,
                    price = i.Price,
                    totalPrice = i.TotalPrice,
                }),
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/merchants/{merchantId}/orders")
            {
                Content = JsonContent.Create(body),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IfoodShippingRequestDriverResult(false, $"Ifood retornou {(int)response.StatusCode}: {Truncate(errorBody)}", null, null);
            }

            var dto = await response.Content.ReadFromJsonAsync<RequestDriverResponseDto>(cancellationToken: cancellationToken);
            return new IfoodShippingRequestDriverResult(true, null, dto?.Id, dto?.TrackingUrl);
        }
        catch (Exception ex)
        {
            return new IfoodShippingRequestDriverResult(false, ex.Message, null, null);
        }
    }

    public async Task<IfoodShippingTrackingResult> GetTrackingAsync(string accessToken, string deliveryId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/orders/{deliveryId}/tracking");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IfoodShippingTrackingResult(false, $"Ifood retornou {(int)response.StatusCode}: {Truncate(body)}", null, null, null, null, null);
            }

            var dto = await response.Content.ReadFromJsonAsync<TrackingResponseDto>(cancellationToken: cancellationToken);
            if (dto is null)
                return new IfoodShippingTrackingResult(false, "Resposta vazia do Ifood.", null, null, null, null, null);

            return new IfoodShippingTrackingResult(true, null, dto.Latitude, dto.Longitude, dto.ExpectedDelivery, dto.DeliveryEtaEnd, dto.PickupEtaStart);
        }
        catch (Exception ex)
        {
            return new IfoodShippingTrackingResult(false, ex.Message, null, null, null, null, null);
        }
    }

    public async Task<IReadOnlyCollection<IfoodShippingCancellationReasonDto>> GetCancellationReasonsAsync(
        string accessToken, string deliveryId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/orders/{deliveryId}/cancellationReasons");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return [];

            var dto = await response.Content.ReadFromJsonAsync<List<ReasonDto>>(cancellationToken: cancellationToken);
            return dto?.Select(r => new IfoodShippingCancellationReasonDto(r.CancelCodeId, r.Description)).ToList()
                   ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<IfoodShippingActionResult> CancelAsync(
        string accessToken, string deliveryId, string reason, int cancellationCode, CancellationToken cancellationToken = default)
        => await PostActionAsync($"{BaseUrl}/orders/{deliveryId}/cancel", accessToken,
            new { reason, cancellationCode }, cancellationToken);

    public async Task<IfoodSafeDeliveryScoreResult> GetSafeDeliveryScoreAsync(
        string accessToken, string deliveryId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/orders/{deliveryId}/safeDelivery");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new IfoodSafeDeliveryScoreResult(false, $"Ifood retornou {(int)response.StatusCode}: {Truncate(body)}", null);
            }

            var dto = await response.Content.ReadFromJsonAsync<SafeDeliveryResponseDto>(cancellationToken: cancellationToken);
            return new IfoodSafeDeliveryScoreResult(true, null, dto?.Score);
        }
        catch (Exception ex)
        {
            return new IfoodSafeDeliveryScoreResult(false, ex.Message, null);
        }
    }

    public async Task<IfoodShippingActionResult> RequestDriverForOrderAsync(
        string accessToken, string IfoodOrderId, string quoteId, CancellationToken cancellationToken = default)
        => await PostActionAsync($"{BaseUrl}/orders/{IfoodOrderId}/requestDriver", accessToken, new { quoteId }, cancellationToken);

    public async Task<IfoodShippingActionResult> CancelDriverForOrderAsync(
        string accessToken, string IfoodOrderId, CancellationToken cancellationToken = default)
        => await PostActionAsync($"{BaseUrl}/orders/{IfoodOrderId}/cancelRequestDriver", accessToken, null, cancellationToken);

    // Fase 11 — fluxo de troca de endereço de entrega em andamento. Os 3 verbos de resposta
    // (accept/deny/userConfirm) não têm body na doc oficial; só o request tem.
    public async Task<IfoodShippingActionResult> RequestDeliveryAddressChangeAsync(
        string accessToken, string IfoodOrderId, IfoodShippingDeliveryAddressChangePayload payload, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            streetNumber = payload.StreetNumber,
            streetName = payload.StreetName,
            complement = payload.Complement,
            neighborhood = payload.Neighborhood,
            city = payload.City,
            state = payload.State,
            country = payload.Country,
            reference = payload.Reference,
            coordinates = payload.Latitude.HasValue && payload.Longitude.HasValue
                ? new { latitude = payload.Latitude.Value, longitude = payload.Longitude.Value }
                : null,
        };
        return await PostActionAsync($"{BaseUrl}/orders/{IfoodOrderId}/deliveryAddressChangeRequest", accessToken, body, cancellationToken);
    }

    public async Task<IfoodShippingActionResult> AcceptDeliveryAddressChangeAsync(
        string accessToken, string IfoodOrderId, CancellationToken cancellationToken = default)
        => await PostActionAsync($"{BaseUrl}/orders/{IfoodOrderId}/acceptDeliveryAddressChange", accessToken, null, cancellationToken);

    public async Task<IfoodShippingActionResult> DenyDeliveryAddressChangeAsync(
        string accessToken, string IfoodOrderId, CancellationToken cancellationToken = default)
        => await PostActionAsync($"{BaseUrl}/orders/{IfoodOrderId}/denyDeliveryAddressChange", accessToken, null, cancellationToken);

    public async Task<IfoodShippingActionResult> ConfirmUserAddressAsync(
        string accessToken, string IfoodOrderId, CancellationToken cancellationToken = default)
        => await PostActionAsync($"{BaseUrl}/orders/{IfoodOrderId}/userConfirmAddress", accessToken, null, cancellationToken);

    private async Task<IfoodShippingActionResult> PostActionAsync(string url, string accessToken, object? payload, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            if (payload is not null)
                request.Content = JsonContent.Create(payload);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return new IfoodShippingActionResult(true, null);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new IfoodShippingActionResult(false, $"Ifood retornou {(int)response.StatusCode}: {Truncate(body)}");
        }
        catch (Exception ex)
        {
            return new IfoodShippingActionResult(false, ex.Message);
        }
    }

    private static string Truncate(string value) => value.Length > 300 ? value[..300] + "…" : value;

    // DTOs internos de desserialização — nomes batem com o JSON do Ifood; ReadFromJsonAsync sem
    // options explícitas já é case-insensitive por padrão (mesmo padrão usado em IfoodOrderClient).
    private sealed record QuoteResponseDto(QuoteDto? Quote, DeliveryTimeDto? DeliveryTime, int? Distance, string? Id, DateTime? ExpirationAt);
    private sealed record QuoteDto(decimal GrossValue, decimal Discount, decimal NetValue);
    private sealed record DeliveryTimeDto(double Min, double Max);
    private sealed record RequestDriverResponseDto(string? Id, string? TrackingUrl);
    private sealed record TrackingResponseDto(double? Latitude, double? Longitude, DateTime? ExpectedDelivery, double? DeliveryEtaEnd, double? PickupEtaStart);
    private sealed record ReasonDto(string CancelCodeId, string Description);
    private sealed record SafeDeliveryResponseDto(string? Score);
}
