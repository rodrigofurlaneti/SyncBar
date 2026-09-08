using System.Net;
using FluentAssertions;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Infrastructure.Integrations.Ifood;
using SyncBar.Tests.Infrastructure.Integrations.TestSupport;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.IFood;

public sealed class IfoodShippingClientTests
{
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly IfoodShippingClient _client;

    public IfoodShippingClientTests()
    {
        _client = new IfoodShippingClient(new HttpClient(_handler));
    }

    [Fact]
    public async Task GetDeliveryAvailabilitiesAsync_Success_ShouldMapQuoteAndSendCoordinatesInQuery()
    {
        _handler.EnqueueJson(HttpStatusCode.OK,
            """{"id":"quote-1","quote":{"grossValue":10,"discount":2,"netValue":8},"deliveryTime":{"min":20,"max":40},"distance":1500,"expirationAt":"2026-01-01T00:00:00Z"}""");

        var result = await _client.GetDeliveryAvailabilitiesAsync("tok", "MERCH-1", -23.5, -46.6, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.QuoteId.Should().Be("quote-1");
        result.NetValue.Should().Be(8);
        result.DeliveryTimeMinMinutes.Should().Be(20);
        result.DistanceMeters.Should().Be(1500);
        var url = _handler.Requests[^1].RequestUri!.ToString();
        url.Should().Contain("Latitude=-23.5").And.Contain("Longitude=-46.6");
    }

    [Fact]
    public async Task GetDeliveryAvailabilitiesAsync_Failure_ShouldReturnFailureResult()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, "endereco fora de area de cobertura");

        var result = await _client.GetDeliveryAvailabilitiesAsync("tok", "MERCH-1", 0, 0, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("404").And.Contain("endereco fora de area de cobertura");
    }

    [Fact]
    public async Task GetDeliveryAvailabilitiesAsync_NullBody_ShouldReturnEmptyResponseFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "null");

        var result = await _client.GetDeliveryAvailabilitiesAsync("tok", "MERCH-1", 0, 0, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("vazia");
    }

    [Fact]
    public async Task GetDeliveryAvailabilitiesAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.GetDeliveryAvailabilitiesAsync("tok", "MERCH-1", 0, 0, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task GetDeliveryAvailabilitiesForOrderAsync_ShouldHitOrderScopedEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"quote-1","quote":{"grossValue":5,"discount":0,"netValue":5},"deliveryTime":{"min":10,"max":20},"distance":500}""");

        var result = await _client.GetDeliveryAvailabilitiesForOrderAsync("tok", "order-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("orders/order-1/deliveryAvailabilities");
    }

    private static IfoodShippingRequestDriverPayload ValidDriverPayload(double? lat = -23.5, double? lng = -46.6) => new(
        CustomerName: "Cliente Teste",
        CustomerPhoneAreaCode: "11",
        CustomerPhoneNumber: "999999999",
        MerchantFee: 5m,
        QuoteId: "quote-1",
        PostalCode: "01310000",
        StreetNumber: "100",
        StreetName: "Av Paulista",
        Complement: null,
        Neighborhood: "Bela Vista",
        City: "Sao Paulo",
        State: "SP",
        Country: "BR",
        Reference: null,
        Latitude: lat,
        Longitude: lng,
        Items: [new IfoodShippingItemPayload("Produto 1", "ext-1", 2, 10m, 10m, 20m)]);

    [Fact]
    public async Task RequestDriverAsync_Success_ShouldPostFullPayloadAndMapResponse()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"delivery-1","trackingUrl":"https://track.example/1"}""");

        var result = await _client.RequestDriverAsync("tok", "MERCH-1", ValidDriverPayload(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.DeliveryId.Should().Be("delivery-1");
        result.TrackingUrl.Should().Be("https://track.example/1");
        var body = _handler.RequestBodies[^1]!;
        body.Should().Contain("\"quoteId\":\"quote-1\"");
        body.Should().Contain("\"streetName\":\"Av Paulista\"");
        body.Should().Contain("\"coordinates\"");
    }

    [Fact]
    public async Task RequestDriverAsync_WithoutCoordinates_ShouldOmitCoordinatesField()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"delivery-1","trackingUrl":null}""");

        await _client.RequestDriverAsync("tok", "MERCH-1", ValidDriverPayload(null, null), CancellationToken.None);

        var body = _handler.RequestBodies[^1]!;
        body.Should().Contain("\"coordinates\":null");
    }

    [Fact]
    public async Task RequestDriverAsync_Failure_ShouldReturnFailureWithStatusAndBody()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "cotacao expirada");

        var result = await _client.RequestDriverAsync("tok", "MERCH-1", ValidDriverPayload(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("400").And.Contain("cotacao expirada");
    }

    [Fact]
    public async Task RequestDriverAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("falha"));

        var result = await _client.RequestDriverAsync("tok", "MERCH-1", ValidDriverPayload(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("falha");
    }

    [Fact]
    public async Task GetTrackingAsync_Success_ShouldMapCoordinatesAndEtas()
    {
        _handler.EnqueueJson(HttpStatusCode.OK,
            """{"latitude":-23.5,"longitude":-46.6,"expectedDelivery":"2026-01-01T12:00:00Z","deliveryEtaEnd":15,"pickupEtaStart":5}""");

        var result = await _client.GetTrackingAsync("tok", "delivery-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Latitude.Should().Be(-23.5);
        result.DeliveryEtaEndMinutes.Should().Be(15d / 60d);
    }

    [Fact]
    public async Task GetTrackingAsync_NotFound_ShouldReturnPendingPosition()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, "entrega nao encontrada");

        var result = await _client.GetTrackingAsync("tok", "delivery-missing", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task GetTrackingAsync_NullBody_ShouldReturnEmptyResponseFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "null");

        var result = await _client.GetTrackingAsync("tok", "delivery-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("vazia");
    }

    [Fact]
    public async Task GetTrackingAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.GetTrackingAsync("tok", "delivery-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task GetCancellationReasonsAsync_Success_ShouldMapReasons()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """[{"cancelCodeId":"1","description":"Cliente cancelou"}]""");

        var result = await _client.GetCancellationReasonsAsync("tok", "delivery-1", CancellationToken.None);

        result.Should().ContainSingle(r => r.CancelCodeId == "1" && r.Description == "Cliente cancelou");
    }

    [Fact]
    public async Task GetCancellationReasonsAsync_Failure_ShouldReturnEmptyList()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var result = await _client.GetCancellationReasonsAsync("tok", "delivery-1", CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCancellationReasonsAsync_NetworkException_ShouldReturnEmptyList()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.GetCancellationReasonsAsync("tok", "delivery-1", CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CancelAsync_Success_ShouldPostReasonAndCode()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.CancelAsync("tok", "delivery-1", "motivo", 7, CancellationToken.None);

        result.Success.Should().BeTrue();
        _handler.RequestBodies[^1].Should().Contain("motivo").And.Contain("7");
    }

    [Fact]
    public async Task CancelAsync_Failure_ShouldReturnFailureWithStatusAndBody()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "codigo invalido");

        var result = await _client.CancelAsync("tok", "delivery-1", "motivo", 7, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("400").And.Contain("codigo invalido");
    }

    [Fact]
    public async Task GetSafeDeliveryScoreAsync_Success_ShouldReturnScore()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"score":"HIGH"}""");

        var result = await _client.GetSafeDeliveryScoreAsync("tok", "delivery-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Score.Should().Be("HIGH");
    }

    [Fact]
    public async Task GetSafeDeliveryScoreAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, "nao encontrado");

        var result = await _client.GetSafeDeliveryScoreAsync("tok", "delivery-1", CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetSafeDeliveryScoreAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.GetSafeDeliveryScoreAsync("tok", "delivery-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task RequestDriverForOrderAsync_ShouldPostQuoteIdToCorrectEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.RequestDriverForOrderAsync("tok", "order-1", "quote-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("orders/order-1/requestDriver");
        _handler.RequestBodies[^1].Should().Contain("quote-1");
    }

    [Fact]
    public async Task CancelDriverForOrderAsync_ShouldPostToCorrectEndpointWithoutBody()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.CancelDriverForOrderAsync("tok", "order-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("orders/order-1/cancelRequestDriver");
        _handler.RequestBodies[^1].Should().BeNull();
    }

    private static IfoodShippingDeliveryAddressChangePayload ValidAddressChangePayload(double? lat = -23.5, double? lng = -46.6) => new(
        StreetNumber: "200", StreetName: "Rua Nova", Complement: null, Neighborhood: "Centro",
        City: "Sao Paulo", State: "SP", Country: "BR", Reference: null, Latitude: lat, Longitude: lng);

    [Fact]
    public async Task RequestDeliveryAddressChangeAsync_ShouldPostNewAddressPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.RequestDeliveryAddressChangeAsync("tok", "order-1", ValidAddressChangePayload(), CancellationToken.None);

        result.Success.Should().BeTrue();
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("orders/order-1/deliveryAddressChangeRequest");
        _handler.RequestBodies[^1].Should().Contain("Rua Nova");
    }

    [Fact]
    public async Task RequestDeliveryAddressChangeAsync_WithoutCoordinates_ShouldOmitCoordinates()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        await _client.RequestDeliveryAddressChangeAsync("tok", "order-1", ValidAddressChangePayload(null, null), CancellationToken.None);

        _handler.RequestBodies[^1].Should().Contain("\"coordinates\":null");
    }

    [Fact]
    public async Task AcceptDeliveryAddressChangeAsync_ShouldPostToCorrectEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.AcceptDeliveryAddressChangeAsync("tok", "order-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("orders/order-1/acceptDeliveryAddressChange");
    }

    [Fact]
    public async Task DenyDeliveryAddressChangeAsync_ShouldPostToCorrectEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.DenyDeliveryAddressChangeAsync("tok", "order-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("orders/order-1/denyDeliveryAddressChange");
    }

    [Fact]
    public async Task ConfirmUserAddressAsync_ShouldPostToCorrectEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.ConfirmUserAddressAsync("tok", "order-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("orders/order-1/userConfirmAddress");
    }
}
