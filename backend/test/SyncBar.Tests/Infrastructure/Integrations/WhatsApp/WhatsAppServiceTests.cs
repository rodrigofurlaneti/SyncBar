using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SyncBar.Infrastructure.Integrations.WhatsApp;
using SyncBar.Tests.Infrastructure.Integrations.TestSupport;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.WhatsApp;

public sealed class WhatsAppServiceTests
{
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly WhatsAppService _service;

    public WhatsAppServiceTests()
    {
        var httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://zap.sistemapocket.com.br/rest-api/") };
        var settings = new WhatsAppSettings { Token = "tok-1", ConnectionId = "202" };
        _service = new WhatsAppService(httpClient, Options.Create(settings), NullLogger<WhatsAppService>.Instance);
    }

    private HttpRequestMessage LastRequest => _handler.Requests[^1];

    [Fact]
    public async Task SendImageAsync_Success_ShouldPostFormEncodedPayloadToSendImageEndpoint()
    {
        _handler.Enqueue(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("OK") });

        var result = await _service.SendImageAsync("5511999999999", "Ola", "https://cdn.example.com/img.jpg", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        LastRequest.Method.Should().Be(HttpMethod.Post);
        LastRequest.RequestUri!.ToString().Should().Be("https://zap.sistemapocket.com.br/rest-api/sendImage");

        var body = _handler.RequestBodies[^1];
        body.Should().Contain("token=tok-1");
        body.Should().Contain("id_conexao=202");
        body.Should().Contain("numero=5511999999999");
        body.Should().Contain("mensagem=Ola");
        body.Should().Contain("fileurl=https%3A%2F%2Fcdn.example.com%2Fimg.jpg");
    }

    [Fact]
    public async Task SendImageAsync_ApiReturnsNonSuccessStatus_ShouldReturnFailure()
    {
        _handler.Enqueue(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("erro interno") });

        var result = await _service.SendImageAsync("5511999999999", "Ola", "https://cdn.example.com/img.jpg", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsApp.SendFailed");
    }

    [Fact]
    public async Task SendImageAsync_NetworkFailure_ShouldReturnFailure()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("conexão recusada"));

        var result = await _service.SendImageAsync("5511999999999", "Ola", "https://cdn.example.com/img.jpg", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsApp.NetworkError");
    }

    [Fact]
    public async Task SendImageAsync_Timeout_ShouldReturnFailure()
    {
        _handler.Enqueue(_ => throw new OperationCanceledException());

        var result = await _service.SendImageAsync("5511999999999", "Ola", "https://cdn.example.com/img.jpg", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WhatsApp.Timeout");
    }
}
