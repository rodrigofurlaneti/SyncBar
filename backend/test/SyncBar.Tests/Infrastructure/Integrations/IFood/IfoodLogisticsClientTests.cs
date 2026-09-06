using System.Net;
using FluentAssertions;
using SyncBar.Infrastructure.Integrations.Ifood;
using SyncBar.Tests.Infrastructure.Integrations.TestSupport;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.IFood;

public sealed class IfoodLogisticsClientTests
{
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly IfoodLogisticsClient _client;

    public IfoodLogisticsClientTests()
    {
        _client = new IfoodLogisticsClient(new HttpClient(_handler));
    }

    [Fact]
    public async Task AssignDriverAsync_Success_ShouldPostWorkerPayloadWithBearerAuth()
    {
        _handler.EnqueueJson(HttpStatusCode.Accepted, "");

        var result = await _client.AssignDriverAsync("tok", "order-1", "Joao Silva", "11999999999", "MOTORCYCLE", CancellationToken.None);

        result.Success.Should().BeTrue();
        var request = _handler.Requests[^1];
        request.RequestUri!.ToString().Should().EndWith("orders/order-1/assignDriver");
        request.Headers.Authorization!.Parameter.Should().Be("tok");
        _handler.RequestBodies[^1].Should().Contain("Joao Silva").And.Contain("MOTORCYCLE");
    }

    [Fact]
    public async Task AssignDriverAsync_Failure_ShouldReturnFailureWithStatusAndBody()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, "pedido não pode ser atribuído");

        var result = await _client.AssignDriverAsync("tok", "order-1", "João", "119", "BIKE", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("400").And.Contain("pedido não pode ser atribuído");
    }

    [Fact]
    public async Task AssignDriverAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.AssignDriverAsync("tok", "order-1", "João", "119", "BIKE", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }

    [Fact]
    public async Task GoingToOriginAsync_ShouldPostToCorrectEndpointWithoutBody()
    {
        _handler.EnqueueJson(HttpStatusCode.Accepted, "");

        var result = await _client.GoingToOriginAsync("tok", "order-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("orders/order-1/goingToOrigin");
        _handler.RequestBodies[^1].Should().BeNull();
    }

    [Fact]
    public async Task ArrivedAtOriginAsync_ShouldPostToCorrectEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.Accepted, "");

        await _client.ArrivedAtOriginAsync("tok", "order-1", CancellationToken.None);

        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("orders/order-1/arrivedAtOrigin");
    }

    [Fact]
    public async Task DispatchAsync_ShouldPostToCorrectEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.Accepted, "");

        await _client.DispatchAsync("tok", "order-1", CancellationToken.None);

        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("orders/order-1/dispatch");
    }

    [Fact]
    public async Task ArrivedAtDestinationAsync_ShouldPostToCorrectEndpoint()
    {
        _handler.EnqueueJson(HttpStatusCode.Accepted, "");

        await _client.ArrivedAtDestinationAsync("tok", "order-1", CancellationToken.None);

        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("orders/order-1/arrivedAtDestination");
    }

    [Fact]
    public async Task VerifyDeliveryCodeAsync_Success_ShouldReturnCodeMatchedFromPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"success":true}""");

        var result = await _client.VerifyDeliveryCodeAsync("tok", "order-1", "1234", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.CodeMatched.Should().BeTrue();
        _handler.RequestBodies[^1].Should().Contain("1234");
    }

    [Fact]
    public async Task VerifyDeliveryCodeAsync_CodeDoesNotMatch_ShouldReturnCodeMatchedFalse()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"success":false}""");

        var result = await _client.VerifyDeliveryCodeAsync("tok", "order-1", "0000", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.CodeMatched.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyDeliveryCodeAsync_PreconditionFailed_ShouldReturnSpecificBusinessMessage()
    {
        _handler.EnqueueJson(HttpStatusCode.PreconditionFailed, "");

        var result = await _client.VerifyDeliveryCodeAsync("tok", "order-1", "1234", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.CodeMatched.Should().BeFalse();
        result.ErrorMessage.Should().Contain("não foi recebido");
    }

    [Fact]
    public async Task VerifyDeliveryCodeAsync_OtherFailure_ShouldReturnGenericMessage()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro interno");

        var result = await _client.VerifyDeliveryCodeAsync("tok", "order-1", "1234", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("500").And.Contain("erro interno");
    }

    [Fact]
    public async Task VerifyDeliveryCodeAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("falha de rede"));

        var result = await _client.VerifyDeliveryCodeAsync("tok", "order-1", "1234", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("falha de rede");
    }

    [Fact]
    public async Task GetOrderDetailsAsync_Success_ShouldReturnRawBody()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"order-1","status":"IN_PROGRESS"}""");

        var result = await _client.GetOrderDetailsAsync("tok", "order-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.RawPayload.Should().Contain("IN_PROGRESS");
    }

    [Fact]
    public async Task GetOrderDetailsAsync_Failure_ShouldReturnFalseWithTruncatedBody()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, "pedido não encontrado");

        var result = await _client.GetOrderDetailsAsync("tok", "order-missing", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.RawPayload.Should().BeNull();
        result.ErrorMessage.Should().Contain("404").And.Contain("pedido não encontrado");
    }

    [Fact]
    public async Task GetOrderDetailsAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("timeout"));

        var result = await _client.GetOrderDetailsAsync("tok", "order-1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("timeout");
    }
}
