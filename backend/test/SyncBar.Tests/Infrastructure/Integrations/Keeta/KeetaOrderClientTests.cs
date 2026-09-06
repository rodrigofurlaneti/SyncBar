using System.Net;
using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Application.Features.Integrations.Keeta.Authorization;
using SyncBar.Domain.Primitives;
using SyncBar.Infrastructure.Integrations.Keeta;
using SyncBar.Tests.Infrastructure.Integrations.TestSupport;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.Keeta;

public sealed class KeetaOrderClientTests
{
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly IKeetaCredentialsResolver _credentialsResolver = Substitute.For<IKeetaCredentialsResolver>();
    private readonly IKeetaAccessTokenProvider _tokenProvider = Substitute.For<IKeetaAccessTokenProvider>();
    private readonly KeetaOrderClient _client;

    public KeetaOrderClientTests()
    {
        _client = new KeetaOrderClient(new HttpClient(_handler), _credentialsResolver, _tokenProvider);
        _credentialsResolver.ResolveAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(new KeetaCredentials("https://open.mykeeta.com/api/open/opendelivery", "client-1", "secret-1", "app-1"));
        _tokenProvider.GetValidAccessTokenAsync(1, 2, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("access-tok"));
    }

    private HttpRequestMessage LastRequest => _handler.Requests[^1];
    private JsonElement LastRequestBody() => JsonDocument.Parse(_handler.RequestBodies[^1]!).RootElement;

    [Fact]
    public async Task ConfirmOrderAsync_ShouldPostToConfirmEndpointWithAuthAndPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");

        await _client.ConfirmOrderAsync(1, 2, "order-1", "SB-1", new DateTime(2026, 1, 1), "motivo", 15, CancellationToken.None);

        LastRequest.Method.Should().Be(HttpMethod.Post);
        LastRequest.RequestUri!.ToString().Should().EndWith("v1/orders/order-1/confirm");
        LastRequest.Headers.Authorization!.Parameter.Should().Be("access-tok");
        var body = LastRequestBody();
        body.GetProperty("orderExternalCode").GetString().Should().Be("SB-1");
        body.GetProperty("reason").GetString().Should().Be("motivo");
        body.GetProperty("preparationTime").GetInt32().Should().Be(15);
    }

    [Fact]
    public async Task ConfirmOrderAsync_TokenResolutionFails_ShouldThrowWithoutCallingHttp()
    {
        _tokenProvider.GetValidAccessTokenAsync(1, 2, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(new Error("Keeta.NoToken", "sem token")));

        var act = () => _client.ConfirmOrderAsync(1, 2, "order-1", "SB-1", DateTime.UtcNow, cancellationToken: CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*sem token*");
        _handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task ConfirmOrderAsync_ApiError_ShouldThrowWithStatusAndBody()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, """{"message":"pedido inválido"}""");

        var act = () => _client.ConfirmOrderAsync(1, 2, "order-1", "SB-1", DateTime.UtcNow, cancellationToken: CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*400*pedido inválido*");
    }

    [Fact]
    public async Task MarkReadyForPickupAsync_ShouldPostWithNullBody()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");

        await _client.MarkReadyForPickupAsync(1, 2, "order-1", CancellationToken.None);

        LastRequest.RequestUri!.ToString().Should().EndWith("v1/orders/order-1/readyForPickup");
    }

    [Fact]
    public async Task DispatchOrderAsync_WithTrackingEvent_ShouldIncludeDeliveryTrackingInfo()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");
        var trackingEvent = new KeetaDeliveryTrackingEvent("DRIVER_ASSIGNED", new DateTime(2026, 1, 1), "Motorista a caminho");

        await _client.DispatchOrderAsync(1, 2, "order-1", trackingEvent, CancellationToken.None);

        var body = LastRequestBody();
        body.GetProperty("deliveryTrackingInfo").GetProperty("event").GetProperty("type").GetString().Should().Be("DRIVER_ASSIGNED");
        body.GetProperty("deliveryTrackingInfo").GetProperty("event").GetProperty("message").GetString().Should().Be("Motorista a caminho");
    }

    [Fact]
    public async Task DispatchOrderAsync_WithoutTrackingEvent_ShouldPostNullBody()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");

        await _client.DispatchOrderAsync(1, 2, "order-1", null, CancellationToken.None);

        LastRequest.RequestUri!.ToString().Should().EndWith("v1/orders/order-1/dispatch");
    }

    [Fact]
    public async Task MarkDeliveredAsync_ShouldPostToDeliveredEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");

        await _client.MarkDeliveredAsync(1, 2, "order-1", CancellationToken.None);

        LastRequest.RequestUri!.ToString().Should().EndWith("v1/orders/order-1/delivered");
    }

    [Fact]
    public async Task SendTrackingUpdateAsync_ShouldPostTrackingPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");
        var trackingEvent = new KeetaDeliveryTrackingEvent("ARRIVED_AT_DESTINATION", new DateTime(2026, 1, 2));

        await _client.SendTrackingUpdateAsync(1, 2, "order-1", trackingEvent, CancellationToken.None);

        LastRequest.RequestUri!.ToString().Should().EndWith("v1/orders/order-1/tracking");
        var body = LastRequestBody();
        body.GetProperty("deliveryTrackingInfo").GetProperty("event").GetProperty("type").GetString().Should().Be("ARRIVED_AT_DESTINATION");
    }

    [Fact]
    public async Task RequestCancellationAsync_ShouldPostReasonCodeModeAndItemLists()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");

        await _client.RequestCancellationAsync(1, 2, "order-1", "sem estoque", "OUT_OF_STOCK", "FULL",
            ["item-1"], ["item-2"], CancellationToken.None);

        var body = LastRequestBody();
        body.GetProperty("reason").GetString().Should().Be("sem estoque");
        body.GetProperty("code").GetString().Should().Be("OUT_OF_STOCK");
        body.GetProperty("mode").GetString().Should().Be("FULL");
        body.GetProperty("outOfStockItems")[0].GetString().Should().Be("item-1");
        body.GetProperty("invalidItems")[0].GetString().Should().Be("item-2");
    }

    [Fact]
    public async Task RequestCancellationAsync_WithoutItemLists_ShouldOmitNullFields()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");

        await _client.RequestCancellationAsync(1, 2, "order-1", "motivo", "CODE", "PARTIAL", cancellationToken: CancellationToken.None);

        var body = LastRequestBody();
        body.TryGetProperty("outOfStockItems", out var outOfStock).Should().BeTrue();
        outOfStock.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task AcceptRefundAsync_ShouldPostToAcceptRefundEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");

        await _client.AcceptRefundAsync(1, 2, "order-1", CancellationToken.None);

        LastRequest.RequestUri!.ToString().Should().EndWith("v1/orders/order-1/acceptRefund");
    }

    [Fact]
    public async Task RejectRefundAsync_ShouldPostReasonAndCode()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");

        await _client.RejectRefundAsync(1, 2, "order-1", "motivo", "CODE", CancellationToken.None);

        var body = LastRequestBody();
        body.GetProperty("reason").GetString().Should().Be("motivo");
        body.GetProperty("code").GetString().Should().Be("CODE");
        LastRequest.RequestUri!.ToString().Should().EndWith("v1/orders/order-1/rejectRefund");
    }

    [Fact]
    public async Task PollEventsAsync_Success_ShouldMapEventsAndSendMerchantHeader()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """
            [
                {"eventId":"evt-1","eventType":"CONFIRMED","orderId":"order-1","orderURL":"https://x","createdAt":"2026-01-01T00:00:00Z"},
                {"eventId":"evt-2","eventType":"DELIVERED","orderId":"order-2","createdAt":"2026-01-02T00:00:00Z"}
            ]
            """);

        var events = await _client.PollEventsAsync(1, 2, ["MERCH-1", "MERCH-2"], CancellationToken.None);

        events.Should().HaveCount(2);
        events[0].EventId.Should().Be("evt-1");
        events[0].OrderUrl.Should().Be("https://x");
        events[1].OrderUrl.Should().BeEmpty();
        events[1].RawJson.Should().Contain("evt-2");
        LastRequest.Headers.GetValues("x-polling-merchants").Should().ContainSingle().Which.Should().Be("MERCH-1,MERCH-2");
        LastRequest.Method.Should().Be(HttpMethod.Get);
        LastRequest.RequestUri!.ToString().Should().EndWith("v1/events:polling");
    }

    [Fact]
    public async Task PollEventsAsync_NoMerchantIds_ShouldNotSendMerchantHeader()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "[]");

        var events = await _client.PollEventsAsync(1, 2, null, CancellationToken.None);

        events.Should().BeEmpty();
        LastRequest.Headers.Contains("x-polling-merchants").Should().BeFalse();
    }

    [Fact]
    public async Task PollEventsAsync_TokenResolutionFails_ShouldThrowWithoutCallingHttp()
    {
        _tokenProvider.GetValidAccessTokenAsync(1, 2, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(new Error("Keeta.NoToken", "sem token")));

        var act = () => _client.PollEventsAsync(1, 2, null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task PollEventsAsync_ApiError_ShouldThrowWithStatusAndBody()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro interno");

        var act = () => _client.PollEventsAsync(1, 2, null, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*500*erro interno*");
    }

    [Fact]
    public async Task AcknowledgeEventsAsync_WithEvents_ShouldPostAckPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");
        var events = new[] { new KeetaPolledEvent("evt-1", "CONFIRMED", "order-1", "https://x", DateTime.UtcNow, "{}") };

        await _client.AcknowledgeEventsAsync(1, 2, events, CancellationToken.None);

        LastRequest.RequestUri!.ToString().Should().EndWith("v1/events/acknowledgment");
        var body = LastRequestBody();
        body[0].GetProperty("id").GetString().Should().Be("evt-1");
        body[0].GetProperty("orderId").GetString().Should().Be("order-1");
        body[0].GetProperty("eventType").GetString().Should().Be("CONFIRMED");
    }

    [Fact]
    public async Task AcknowledgeEventsAsync_NoEvents_ShouldNotCallHttp()
    {
        await _client.AcknowledgeEventsAsync(1, 2, [], CancellationToken.None);

        _handler.Requests.Should().BeEmpty();
    }
}
