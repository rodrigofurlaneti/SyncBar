using System.Net;
using FluentAssertions;
using NSubstitute;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Integrations.Ifood;
using SyncBar.Tests.Infrastructure.Integrations.TestSupport;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.IFood;

public sealed class IfoodOrderClientTests
{
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IfoodOrderClient _client;

    public IfoodOrderClientTests()
    {
        _client = new IfoodOrderClient(new HttpClient(_handler), _logRepository, _unitOfWork);
    }

    [Fact]
    public async Task PollEventsAsync_EmptyMerchantIds_ShouldReturnEmptyWithoutCallingHttp()
    {
        var events = await _client.PollEventsAsync("tok", [], CancellationToken.None);

        events.Should().BeEmpty();
        _handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task PollEventsAsync_Success_ShouldMapEventsAndSendMerchantHeaderAndCategories()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """
            [{"id":"evt-1","code":"PLACED","fullCode":"PLC","orderId":"order-1","createdAt":"2026-01-01T00:00:00Z"}]
            """);

        var events = await _client.PollEventsAsync("tok", ["MERCH-1", "MERCH-2"], CancellationToken.None);

        events.Should().ContainSingle(e => e.Id == "evt-1" && e.OrderId == "order-1");
        var request = _handler.Requests[^1];
        request.Headers.GetValues("x-polling-merchants").Should().ContainSingle().Which.Should().Be("MERCH-1,MERCH-2");
        request.RequestUri!.ToString().Should().Contain("categories=FOOD").And.Contain("FOOD_SELF_SERVICE");
    }

    [Fact]
    public async Task PollEventsAsync_NoContent_ShouldReturnEmptyWithoutError()
    {
        _handler.Enqueue(new HttpResponseMessage(HttpStatusCode.NoContent));

        var events = await _client.PollEventsAsync("tok", ["MERCH-1"], CancellationToken.None);

        events.Should().BeEmpty();
        await _logRepository.DidNotReceive().AddAsync(Arg.Any<SyncBar.Domain.Entities.LogTracker>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PollEventsAsync_HttpFailure_ShouldSwallowLogAndReturnEmpty()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro interno");

        var events = await _client.PollEventsAsync("tok", ["MERCH-1"], CancellationToken.None);

        events.Should().BeEmpty();
        await _logRepository.Received(1).AddAsync(Arg.Is<SyncBar.Domain.Entities.LogTracker>(l => !l.IsSuccess), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PollEventsAsync_MoreThanBatchSize_ShouldSplitIntoMultipleRequests()
    {
        _handler.DefaultResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.NoContent);
        var merchantIds = Enumerable.Range(1, 150).Select(i => $"M{i}").ToList();

        await _client.PollEventsAsync("tok", merchantIds, CancellationToken.None);

        _handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task AcknowledgeEventsAsync_EmptyIds_ShouldNotCallHttp()
    {
        await _client.AcknowledgeEventsAsync("tok", [], CancellationToken.None);

        _handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task AcknowledgeEventsAsync_WithIds_ShouldPostAcknowledgedEventIds()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");

        await _client.AcknowledgeEventsAsync("tok", ["evt-1", "evt-2"], CancellationToken.None);

        _handler.RequestBodies[^1].Should().Contain("evt-1").And.Contain("evt-2");
    }

    [Fact]
    public async Task AcknowledgeEventsAsync_NetworkException_ShouldBeSwallowed()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var act = () => _client.AcknowledgeEventsAsync("tok", ["evt-1"], CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetOrderDetailsAsync_Success_ShouldMapNestedFieldsAndItemOptions()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """
            {"id":"order-1","displayId":"#1","orderType":"DELIVERY","orderTiming":"IMMEDIATE","category":"FOOD",
             "createdAt":"2026-01-01T00:00:00Z","merchant":{"id":"MERCH-1","name":"Loja"},
             "customer":{"name":"Cliente X","phone":{"number":"11999999999"}},
             "delivery":{"deliveredBy":"IFOOD","deliveryAddress":{"formattedAddress":"Av Paulista, 100"}},
             "total":{"subTotal":10,"deliveryFee":2,"additionalFees":0,"orderAmount":12},
             "items":[{"externalCode":"ext-1","name":"Pizza","quantity":1,"unitPrice":10,
                       "options":[{"id":"opt-1","name":"Borda recheada","quantity":1,"unitPrice":2}]}]}
            """);

        var result = await _client.GetOrderDetailsAsync("tok", "order-1", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be("order-1");
        result.MerchantId.Should().Be("MERCH-1");
        result.CustomerName.Should().Be("Cliente X");
        result.DeliveryAddressFormatted.Should().Be("Av Paulista, 100");
        result.OrderAmount.Should().Be(12);
        result.Items.Should().ContainSingle();
        result.Items.Single().Options.Should().ContainSingle(o => o.Id == "opt-1" && o.Name == "Borda recheada");
    }

    [Fact]
    public async Task GetOrderDetailsAsync_MissingOptionalNestedObjects_ShouldUseDefaults()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"order-1","orderType":"DELIVERY","createdAt":"2026-01-01T00:00:00Z"}""");

        var result = await _client.GetOrderDetailsAsync("tok", "order-1", CancellationToken.None);

        result.Should().NotBeNull();
        result!.OrderTiming.Should().Be("IMMEDIATE");
        result.Category.Should().Be("FOOD");
        result.MerchantId.Should().Be("");
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrderDetailsAsync_Failure_ShouldReturnNull()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, "");

        var result = await _client.GetOrderDetailsAsync("tok", "order-missing", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ConfirmOrderAsync_Success_ShouldPostToConfirmEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.ConfirmOrderAsync("tok", "order-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("orders/order-1/confirm");
    }

    [Fact]
    public async Task ConfirmOrderAsync_Failure_ShouldReturnFailureWithStatusAndBody()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "pedido ja confirmado");

        var result = await _client.ConfirmOrderAsync("tok", "order-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("400").And.Contain("pedido ja confirmado");
    }

    [Fact]
    public async Task ConfirmOrderAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.ConfirmOrderAsync("tok", "order-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task StartPreparationAsync_ShouldPostToCorrectEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        await _client.StartPreparationAsync("tok", "order-1", CancellationToken.None);

        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("orders/order-1/startPreparation");
    }

    [Fact]
    public async Task ReadyToPickupAsync_ShouldPostToCorrectEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        await _client.ReadyToPickupAsync("tok", "order-1", CancellationToken.None);

        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("orders/order-1/readyToPickup");
    }

    [Fact]
    public async Task DispatchAsync_ShouldPostToCorrectEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        await _client.DispatchAsync("tok", "order-1", CancellationToken.None);

        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("orders/order-1/dispatch");
    }

    [Fact]
    public async Task GetCancellationReasonsAsync_Success_ShouldMapReasons()
    {
        // Resposta real do Ifood é um array bruto de {cancelCodeId, description}, sem envelope.
        _handler.EnqueueJson(HttpStatusCode.OK, """[{"cancelCodeId":"1","description":"Cliente desistiu"}]""");

        var result = await _client.GetCancellationReasonsAsync("tok", "order-1", CancellationToken.None);

        result.Should().ContainSingle(r => r.Code == "1" && r.Description == "Cliente desistiu");
    }

    [Fact]
    public async Task GetCancellationReasonsAsync_NoContent_ShouldReturnEmpty()
    {
        _handler.EnqueueJson(HttpStatusCode.NoContent, "");

        var result = await _client.GetCancellationReasonsAsync("tok", "order-1", CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCancellationReasonsAsync_Failure_ShouldReturnEmpty()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro");

        var result = await _client.GetCancellationReasonsAsync("tok", "order-1", CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RequestCancellationAsync_Success_ShouldPostReasonCode()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.RequestCancellationAsync("tok", "order-1", "501", CancellationToken.None);

        result.Success.Should().BeTrue();
        _handler.RequestBodies[^1].Should().Contain("501");
    }

    [Fact]
    public async Task RequestCancellationAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "motivo invalido");

        var result = await _client.RequestCancellationAsync("tok", "order-1", "999", CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrderTrackingAsync_Success_ShouldMapCoordinatesAndEtas()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"latitude":-23.5,"longitude":-46.6,"expectedDelivery":"2026-01-01T12:00:00Z","deliveryEtaEnd":10,"pickupEtaStart":3}""");

        var result = await _client.GetOrderTrackingAsync("tok", "order-1", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Latitude.Should().Be(-23.5);
        result.DeliveryEtaEndMinutes.Should().Be(10);
    }

    [Fact]
    public async Task GetOrderTrackingAsync_Failure_ShouldReturnNull()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, "");

        var result = await _client.GetOrderTrackingAsync("tok", "order-1", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidatePickupCodeAsync_Success_ShouldReturnCodeMatched()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"success":true}""");

        var result = await _client.ValidatePickupCodeAsync("tok", "order-1", "1234", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.CodeMatched.Should().BeTrue();
        _handler.RequestBodies[^1].Should().Contain("1234");
    }

    [Fact]
    public async Task ValidatePickupCodeAsync_EmptyBody_ShouldReturnSuccessWithCodeMatchedFalse()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.ValidatePickupCodeAsync("tok", "order-1", "1234", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.CodeMatched.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePickupCodeAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "codigo invalido");

        var result = await _client.ValidatePickupCodeAsync("tok", "order-1", "0000", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("400");
    }

    [Fact]
    public async Task ValidatePickupCodeAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.ValidatePickupCodeAsync("tok", "order-1", "1234", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task AcceptDisputeAsync_Success_ShouldMapStatus()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"status":"ACCEPTED"}""");

        var result = await _client.AcceptDisputeAsync("tok", "dispute-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Status.Should().Be("ACCEPTED");
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("disputes/dispute-1/accept");
    }

    [Fact]
    public async Task AcceptDisputeAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "disputa ja resolvida");

        var result = await _client.AcceptDisputeAsync("tok", "dispute-1", CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task AcceptDisputeAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.AcceptDisputeAsync("tok", "dispute-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task RejectDisputeAsync_ShouldPostReasonAndMapStatus()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"status":"REJECTED"}""");

        var result = await _client.RejectDisputeAsync("tok", "dispute-1", "sem evidencias", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Status.Should().Be("REJECTED");
        _handler.RequestBodies[^1].Should().Contain("sem evidencias");
    }

    [Fact]
    public async Task RequestDisputeAlternativeAsync_WithAmount_ShouldIncludeMetadata()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"status":"PROPOSED"}""");

        var result = await _client.RequestDisputeAlternativeAsync("tok", "dispute-1", "alt-1", "REFUND_ITEMS", 15.5m, "BRL", CancellationToken.None);

        result.Success.Should().BeTrue();
        var body = _handler.RequestBodies[^1]!;
        body.Should().Contain("15.5").And.Contain("BRL").And.Contain("REFUND_ITEMS");
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("disputes/dispute-1/alternatives/alt-1");
    }

    [Fact]
    public async Task RequestDisputeAlternativeAsync_WithoutAmount_ShouldOmitMetadata()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"status":"PROPOSED"}""");

        await _client.RequestDisputeAlternativeAsync("tok", "dispute-1", "alt-1", "RESCHEDULE", null, null, CancellationToken.None);

        _handler.RequestBodies[^1].Should().NotContain("metadata");
    }

    [Fact]
    public async Task GetVirtualBagAsync_Success_ShouldParseNestedItemsAndGrossValue()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """
            {"id":"bag-1","shortCode":"SB1","status":"OPEN","createdAt":"2026-01-01T00:00:00Z",
             "merchant":{"name":"Loja X"},"customer":{"name":"Cliente Y"},
             "bag":{"items":[{"uniqueId":"i-1","name":"Produto","quantity":2,"ean":"789"}],
                    "prices":{"grossValue":{"value":50,"currency":"BRL"}}}}
            """);

        var result = await _client.GetVirtualBagAsync("tok", "order-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Id.Should().Be("bag-1");
        result.MerchantName.Should().Be("Loja X");
        result.CustomerName.Should().Be("Cliente Y");
        result.Items.Should().ContainSingle(i => i.UniqueId == "i-1" && i.Quantity == 2);
        result.GrossValueCurrency.Should().Be("BRL");
    }

    [Fact]
    public async Task GetVirtualBagAsync_WithoutBag_ShouldReturnEmptyItemsAndNullGrossValue()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"bag-1","status":"OPEN"}""");

        var result = await _client.GetVirtualBagAsync("tok", "order-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Items.Should().BeEmpty();
        result.GrossValueAmount.Should().BeNull();
    }

    [Fact]
    public async Task GetVirtualBagAsync_Failure_ShouldReturnFailureResult()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, "nao encontrado");

        var result = await _client.GetVirtualBagAsync("tok", "order-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("404");
    }

    [Fact]
    public async Task GetVirtualBagAsync_MalformedJson_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "not-json");

        var result = await _client.GetVirtualBagAsync("tok", "order-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RequestOrderDriverAsync_ShouldPostToCorrectEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.RequestOrderDriverAsync("tok", "order-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("orders/order-1/requestDriver");
    }

    [Fact]
    public async Task CancelOrderRequestDriverAsync_ShouldPostToCorrectEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "");

        var result = await _client.CancelOrderRequestDriverAsync("tok", "order-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("orders/order-1/cancelRequestDriver");
    }

    [Fact]
    public async Task VerifyOrderDeliveryCodeAsync_Success_ShouldReturnCodeMatched()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"success":true}""");

        var result = await _client.VerifyOrderDeliveryCodeAsync("tok", "order-1", "9999", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.CodeMatched.Should().BeTrue();
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("orders/order-1/verifyDeliveryCode");
    }

    [Fact]
    public async Task VerifyOrderDeliveryCodeAsync_Failure_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "codigo invalido");

        var result = await _client.VerifyOrderDeliveryCodeAsync("tok", "order-1", "0000", CancellationToken.None);

        result.Success.Should().BeFalse();
    }
}
