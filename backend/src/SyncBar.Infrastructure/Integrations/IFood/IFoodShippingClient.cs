using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SyncBar.Application.Abstractions.Integrations.IFood;

namespace SyncBar.Infrastructure.Integrations.IFood;

/// <summary>
/// Cliente HTTP real do módulo Shipping do iFood (fase 8, entrega via malha do iFood pra pedidos
/// de outros canais) — endpoints e formatos confirmados em 2026-08-20 contra a documentação
/// oficial (Postman collection "Shipping") colada pelo usuário. Base URL própria
/// (shipping/v1.0) — diferente de order/v1.0 e logistics/v1.0, mesmo padrão de "nome de endpoint
/// parecido, módulo diferente" já visto entre Order.dispatch e Logistics.dispatch (fase 7).
/// </summary>
internal sealed class IFoodShippingClient(HttpClient httpClient) : IIFoodShippingClient
{
    private const string BaseUrl = "https://merchant-api.ifood.com.br/shipping/v1.0";

    public async Task<IFoodShippingQuoteResult> GetDeliveryAvailabilitiesAsync(
        string accessToken, string merchantId, double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/merchants/{merchantId}/deliveryAvailabilities" +
                   $"?Latitude={latitude.ToString(CultureInfo.InvariantCulture)}&Longitude={longitude.ToString(CultureInfo.InvariantCulture)}";
        return await GetQuoteAsync(url, accessToken, cancellationToken);
    }

    public async Task<IFoodShippingQuoteResult> GetDeliveryAvailabilitiesForOrderAsync(
        string accessToken, string ifoodOrderId, CancellationToken cancellationToken = default)
        => await GetQuoteAsync($"{BaseUrl}/orders/{ifoodOrderId}/deliveryAvailabilities", accessToken, cancellationToken);

    private async Task<IFoodShippingQuoteResult> GetQuoteAsync(string url, string accessToken, CancellationToken cancellationToken)
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
                return new IFoodShippingQuoteResult(false, $"iFood retornou {(int)response.StatusCode}: {Truncate(body)}",
                    null, 0, 0, 0, 0, 0, 0, null);
            }

            var dto = await response.Content.ReadFromJsonAsync<QuoteResponseDto>(cancellationToken: cancellationToken);
            if (dto is null)
                return new IFoodShippingQuoteResult(false, "Resposta vazia do iFood.", null, 0, 0, 0, 0, 0, 0, null);

            return new IFoodShippingQuoteResult(
                true, null, dto.Id,
                dto.Quote?.GrossValue ?? 0, dto.Quote?.Discount ?? 0, dto.Quote?.NetValue ?? 0,
                dto.DeliveryTime?.Min ?? 0, dto.DeliveryTime?.Max ?? 0, dto.Distance ?? 0, dto.ExpirationAt);
        }
        catch (Exception ex)
        {
            return new IFoodShippingQuoteResult(false, ex.Message, null, 0, 0, 0, 0, 0, 0, null);
        }
    }

    public async Task<IFoodShippingRequestDriverResult> RequestDriverAsync(
        string accessToken, string merchantId, IFoodShippingRequestDriverPayload payload, CancellationToken cancellationToken = default)
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
                return new IFoodShippingRequestDriverResult(false, $"iFood retornou {(int)response.StatusCode}: {Truncate(errorBody)}", null, null);
            }

            var dto = await response.Content.ReadFromJsonAsync<RequestDriverResponseDto>(cancellationToken: cancellationToken);
            return new IFoodShippingRequestDriverResult(true, null, dto?.Id, dto?.TrackingUrl);
        }
        catch (Exception ex)
        {
            return new IFoodShippingRequestDriverResult(false, ex.Message, null, null);
        }
    }

    public async Task<IFoodShippingTrackingResult> GetTrackingAsync(string accessToken, string deliveryId, CancellationToken cancellationToken = default)
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
                return new IFoodShippingTrackingResult(false, $"iFood retornou {(int)response.StatusCode}: {Truncate(body)}", null, null, null, null, null);
            }

            var dto = await response.Content.ReadFromJsonAsync<TrackingResponseDto>(cancellationToken: cancellationToken);
            if (dto is null)
                return new IFoodShippingTrackingResult(false, "Resposta vazia do iFood.", null, null, null, null, null);

            return new IFoodShippingTrackingResult(true, null, dto.Latitude, dto.Longitude, dto.ExpectedDelivery, dto.DeliveryEtaEnd, dto.PickupEtaStart);
        }
        catch (Exception ex)
        {
            return new IFoodShippingTrackingResult(false, ex.Message, null, null, null, null, null);
        }
    }

    public async Task<IReadOnlyCollection<IFoodShippingCancellationReasonDto>> GetCancellationReasonsAsync(
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
            return dto?.Select(r => new IFoodShippingCancellationReasonDto(r.CancelCodeId, r.Description)).ToList()
                   ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<IFoodShippingActionResult> CancelAsync(
        string accessToken, string deliveryId, string reason, int cancellationCode, CancellationToken cancellationToken = default)
        => await PostActionAsync($"{BaseUrl}/orders/{deliveryId}/cancel", accessToken,
            new { reason, cancellationCode }, cancellationToken);

    public async Task<IFoodSafeDeliveryScoreResult> GetSafeDeliveryScoreAsync(
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
                return new IFoodSafeDeliveryScoreResult(false, $"iFood retornou {(int)response.StatusCode}: {Truncate(body)}", null);
            }

            var dto = await response.Content.ReadFromJsonAsync<SafeDeliveryResponseDto>(cancellationToken: cancellationToken);
            return new IFoodSafeDeliveryScoreResult(true, null, dto?.Score);
        }
        catch (Exception ex)
        {
            return new IFoodSafeDeliveryScoreResult(false, ex.Message, null);
        }
    }

    public async Task<IFoodShippingActionResult> RequestDriverForOrderAsync(
        string accessToken, string ifoodOrderId, string quoteId, CancellationToken cancellationToken = default)
        => await PostActionAsync($"{BaseUrl}/orders/{ifoodOrderId}/requestDriver", accessToken, new { quoteId }, cancellationToken);

    public async Task<IFoodShippingActionResult> CancelDriverForOrderAsync(
        string accessToken, string ifoodOrderId, CancellationToken cancellationToken = default)
        => await PostActionAsync($"{BaseUrl}/orders/{ifoodOrderId}/cancelRequestDriver", accessToken, null, cancellationToken);

    // Fase 11 — fluxo de troca de endereço de entrega em andamento. Os 3 verbos de resposta
    // (accept/deny/userConfirm) não têm body na doc oficial; só o request tem.
    public async Task<IFoodShippingActionResult> RequestDeliveryAddressChangeAsync(
        string accessToken, string ifoodOrderId, IFoodShippingDeliveryAddressChangePayload payload, CancellationToken cancellationToken = default)
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
        return await PostActionAsync($"{BaseUrl}/orders/{ifoodOrderId}/deliveryAddressChangeRequest", accessToken, body, cancellationToken);
    }

    public async Task<IFoodShippingActionResult> AcceptDeliveryAddressChangeAsync(
        string accessToken, string ifoodOrderId, CancellationToken cancellationToken = default)
        => await PostActionAsync($"{BaseUrl}/orders/{ifoodOrderId}/acceptDeliveryAddressChange", accessToken, null, cancellationToken);

    public async Task<IFoodShippingActionResult> DenyDeliveryAddressChangeAsync(
        string accessToken, string ifoodOrderId, CancellationToken cancellationToken = default)
        => await PostActionAsync($"{BaseUrl}/orders/{ifoodOrderId}/denyDeliveryAddressChange", accessToken, null, cancellationToken);

    public async Task<IFoodShippingActionResult> ConfirmUserAddressAsync(
        string accessToken, string ifoodOrderId, CancellationToken cancellationToken = default)
        => await PostActionAsync($"{BaseUrl}/orders/{ifoodOrderId}/userConfirmAddress", accessToken, null, cancellationToken);

    private async Task<IFoodShippingActionResult> PostActionAsync(string url, string accessToken, object? payload, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            if (payload is not null)
                request.Content = JsonContent.Create(payload);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return new IFoodShippingActionResult(true, null);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new IFoodShippingActionResult(false, $"iFood retornou {(int)response.StatusCode}: {Truncate(body)}");
        }
        catch (Exception ex)
        {
            return new IFoodShippingActionResult(false, ex.Message);
        }
    }

    private static string Truncate(string value) => value.Length > 300 ? value[..300] + "…" : value;

    // DTOs internos de desserialização — nomes batem com o JSON do iFood; ReadFromJsonAsync sem
    // options explícitas já é case-insensitive por padrão (mesmo padrão usado em IFoodOrderClient).
    private sealed record QuoteResponseDto(QuoteDto? Quote, DeliveryTimeDto? DeliveryTime, int? Distance, string? Id, DateTime? ExpirationAt);
    private sealed record QuoteDto(decimal GrossValue, decimal Discount, decimal NetValue);
    private sealed record DeliveryTimeDto(double Min, double Max);
    private sealed record RequestDriverResponseDto(string? Id, string? TrackingUrl);
    private sealed record TrackingResponseDto(double? Latitude, double? Longitude, DateTime? ExpectedDelivery, double? DeliveryEtaEnd, double? PickupEtaStart);
    private sealed record ReasonDto(string CancelCodeId, string Description);
    private sealed record SafeDeliveryResponseDto(string? Score);
}
