using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Receive;
using SyncBar.Domain.Primitives;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class AsaasWebhookReceiverControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AsaasWebhookReceiverController _controller;

    public AsaasWebhookReceiverControllerTests()
    {
        _controller = new AsaasWebhookReceiverController(_mediator);
    }

    private void SetupRequest()
    {
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task Receive_Success_ShouldForwardRawPayloadAndTokenAndReturnOk()
    {
        SetupRequest();
        _mediator.Send(Arg.Is<ReceiveAsaasWebhookCommand>(c => c.RawPayload.Contains("PAYMENT_CONFIRMED") && c.AccessToken == "token-1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Receive(JsonDocument.Parse("""{"event":"PAYMENT_CONFIRMED"}""").RootElement, "token-1", CancellationToken.None);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Receive_InvalidToken_ShouldReturnUnauthorized()
    {
        SetupRequest();
        _mediator.Send(Arg.Any<ReceiveAsaasWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Asaas.InvalidWebhookToken", "token invalido")));

        var result = await _controller.Receive(JsonDocument.Parse("{}").RootElement, "bad-token", CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Receive_OtherFailure_ShouldStillReturnOk()
    {
        SetupRequest();
        _mediator.Send(Arg.Any<ReceiveAsaasWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Asaas.ParseError", "payload invalido")));

        var result = await _controller.Receive(JsonDocument.Parse("{}").RootElement, null, CancellationToken.None);

        result.Should().BeOfType<OkResult>();
    }
}
