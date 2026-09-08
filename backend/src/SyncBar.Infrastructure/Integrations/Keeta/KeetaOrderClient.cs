using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Features.Integrations.Keeta.Authorization;

namespace SyncBar.Infrastructure.Integrations.Keeta;

public sealed class KeetaOrderClient : IKeetaOrderClient
{
    private readonly HttpClient _http;
    private readonly IKeetaCredentialsResolver _credentialsResolver;
    private readonly IKeetaAccessTokenProvider _tokenProvider;

    public KeetaOrderClient(HttpClient httpClient, IKeetaCredentialsResolver credentialsResolver, IKeetaAccessTokenProvider tokenProvider)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _credentialsResolver = credentialsResolver;
        _tokenProvider = tokenProvider;

        if (_http.DefaultRequestHeaders.Accept.Count == 0)
        {
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }

    public Task ConfirmOrderAsync(
        long companyId,
        long branchId,
        string keetaOrderId,
        string orderExternalCode,
        DateTime createdAtUtc,
        string? reason = null,
        int? preparationTimeMinutes = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new OrderConfirmedDto
        {
            OrderExternalCode = orderExternalCode,
            CreatedAt = createdAtUtc,
            Reason = reason,
            PreparationTime = preparationTimeMinutes
        };

        return PostAsync(companyId, branchId, $"v1/orders/{keetaOrderId}/confirm", payload, cancellationToken);
    }

    public Task MarkReadyForPickupAsync(long companyId, long branchId, string keetaOrderId, CancellationToken cancellationToken = default) =>
        PostAsync<object?>(companyId, branchId, $"v1/orders/{keetaOrderId}/readyForPickup", null, cancellationToken);

    public Task DispatchOrderAsync(
        long companyId,
        long branchId,
        string keetaOrderId,
        KeetaDeliveryTrackingEvent? trackingEvent = null,
        CancellationToken cancellationToken = default)
    {
        var payload = trackingEvent is null
            ? null
            : new OrderDispatchedDto
            {
                DeliveryTrackingInfo = new DeliveryTrackingInfoDto
                {
                    Event = new DeliveryTrackingEventDto
                    {
                        Type = trackingEvent.Type,
                        Message = trackingEvent.Message,
                        DateTime = trackingEvent.DateTimeUtc
                    }
                }
            };

        return PostAsync(companyId, branchId, $"v1/orders/{keetaOrderId}/dispatch", payload, cancellationToken);
    }

    public Task MarkDeliveredAsync(long companyId, long branchId, string keetaOrderId, CancellationToken cancellationToken = default) =>
        PostAsync<object?>(companyId, branchId, $"v1/orders/{keetaOrderId}/delivered", null, cancellationToken);

    public Task SendTrackingUpdateAsync(
        long companyId,
        long branchId,
        string keetaOrderId,
        KeetaDeliveryTrackingEvent trackingEvent,
        CancellationToken cancellationToken = default)
    {
        var payload = new OrderTrackingDto
        {
            DeliveryTrackingInfo = new DeliveryTrackingInfoDto
            {
                Event = new DeliveryTrackingEventDto
                {
                    Type = trackingEvent.Type,
                    Message = trackingEvent.Message,
                    DateTime = trackingEvent.DateTimeUtc
                }
            }
        };

        return PostAsync(companyId, branchId, $"v1/orders/{keetaOrderId}/tracking", payload, cancellationToken);
    }

    public Task RequestCancellationAsync(
        long companyId,
        long branchId,
        string keetaOrderId,
        string reason,
        string code,
        string mode,
        IReadOnlyList<string>? outOfStockItems = null,
        IReadOnlyList<string>? invalidItems = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new RequestCancelledDto
        {
            Reason = reason,
            Code = code,
            Mode = mode,
            OutOfStockItems = outOfStockItems?.ToList(),
            InvalidItems = invalidItems?.ToList()
        };

        return PostAsync(companyId, branchId, $"v1/orders/{keetaOrderId}/requestCancellation", payload, cancellationToken);
    }

    public Task AcceptRefundAsync(long companyId, long branchId, string keetaOrderId, CancellationToken cancellationToken = default) =>
        PostAsync<object?>(companyId, branchId, $"v1/orders/{keetaOrderId}/acceptRefund", null, cancellationToken);

    public Task RejectRefundAsync(
        long companyId,
        long branchId,
        string keetaOrderId,
        string reason,
        string code,
        CancellationToken cancellationToken = default)
    {
        var payload = new RequestDeniedDto { Reason = reason, Code = code };
        return PostAsync(companyId, branchId, $"v1/orders/{keetaOrderId}/rejectRefund", payload, cancellationToken);
    }

    public async Task<IReadOnlyList<KeetaPolledEvent>> PollEventsAsync(
        long companyId,
        long branchId,
        IReadOnlyList<string>? merchantIds = null,
        CancellationToken cancellationToken = default)
    {
        var credentials = await _credentialsResolver.ResolveAsync(companyId, branchId, cancellationToken);

        var tokenResult = await _tokenProvider.GetValidAccessTokenAsync(companyId, branchId, cancellationToken: cancellationToken);
        if (tokenResult.IsFailure)
            throw new InvalidOperationException($"Não foi possível obter um access_token Keeta válido: {tokenResult.Error.Message}");

        using var request = KeetaSignedRequest.Create(credentials, HttpMethod.Get, "v1/events:polling");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);

        if (merchantIds is { Count: > 0 })
        {
            request.Headers.Add("x-polling-merchants", string.Join(",", merchantIds));
        }

        var response = await _http.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Keeta API retornou {(int)response.StatusCode} ({response.StatusCode}) em v1/events:polling: {body}");
        }

        // Deserializado como JsonElement (em vez de um DTO fortemente tipado) para preservar o
        // JSON bruto de cada evento — o schema "Event" tem campos adicionais por eventType
        // (discriminated union) que não modelamos individualmente, mas que o log de eventos
        // (KeetaIntegrationOrderEventLog.RawPayload) precisa guardar por completo.
        var rawEvents = await response.Content.ReadFromJsonAsync<List<System.Text.Json.JsonElement>>(cancellationToken: cancellationToken) ?? [];

        var events = new List<KeetaPolledEvent>();
        foreach (var element in rawEvents)
        {
            var eventId = element.GetProperty("eventId").GetString() ?? string.Empty;
            var eventType = element.GetProperty("eventType").GetString() ?? string.Empty;
            var orderId = element.GetProperty("orderId").GetString() ?? string.Empty;
            var orderUrl = element.TryGetProperty("orderURL", out var urlProp) ? urlProp.GetString() ?? string.Empty : string.Empty;
            var createdAt = element.GetProperty("createdAt").GetDateTime();

            events.Add(new KeetaPolledEvent(eventId, eventType, orderId, orderUrl, createdAt, element.GetRawText()));
        }

        return events;
    }

    public async Task AcknowledgeEventsAsync(
        long companyId,
        long branchId,
        IReadOnlyList<KeetaPolledEvent> events,
        CancellationToken cancellationToken = default)
    {
        if (events.Count == 0)
            return;

        var payload = events
            .Select(e => new AckEventDto { Id = e.EventId, OrderId = e.OrderId, EventType = e.EventType })
            .ToList();

        await PostAsync(companyId, branchId, "v1/events/acknowledgment", payload, cancellationToken);
    }

    private async Task PostAsync<TPayload>(long companyId, long branchId, string relativeUrl, TPayload? payload, CancellationToken cancellationToken)
    {
        var credentials = await _credentialsResolver.ResolveAsync(companyId, branchId, cancellationToken);

        var tokenResult = await _tokenProvider.GetValidAccessTokenAsync(companyId, branchId, cancellationToken: cancellationToken);
        if (tokenResult.IsFailure)
            throw new InvalidOperationException($"Não foi possível obter um access_token Keeta válido: {tokenResult.Error.Message}");

        using var request = KeetaSignedRequest.Create(credentials, HttpMethod.Post, relativeUrl, payload);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Value);

        var response = await _http.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Keeta API retornou {(int)response.StatusCode} ({response.StatusCode}) em {relativeUrl}: {body}");
        }
    }

    private sealed class OrderConfirmedDto
    {
        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("orderExternalCode")]
        public string OrderExternalCode { get; set; } = string.Empty;

        [JsonPropertyName("preparationTime")]
        public int? PreparationTime { get; set; }
    }

    private sealed class OrderDispatchedDto
    {
        [JsonPropertyName("deliveryTrackingInfo")]
        public DeliveryTrackingInfoDto? DeliveryTrackingInfo { get; set; }
    }

    private sealed class OrderTrackingDto
    {
        [JsonPropertyName("deliveryTrackingInfo")]
        public DeliveryTrackingInfoDto DeliveryTrackingInfo { get; set; } = new();
    }

    private sealed class DeliveryTrackingInfoDto
    {
        [JsonPropertyName("event")]
        public DeliveryTrackingEventDto? Event { get; set; }
    }

    private sealed class DeliveryTrackingEventDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("datetime")]
        public DateTime DateTime { get; set; }
    }

    private sealed class RequestCancelledDto
    {
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = string.Empty;

        [JsonPropertyName("outOfStockItems")]
        public List<string>? OutOfStockItems { get; set; }

        [JsonPropertyName("invalidItems")]
        public List<string>? InvalidItems { get; set; }
    }

    private sealed class RequestDeniedDto
    {
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
    }

    private sealed class AckEventDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("eventType")]
        public string EventType { get; set; } = string.Empty;
    }
}
