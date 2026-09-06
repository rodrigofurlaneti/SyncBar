using System.Text;
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

    private void SetupRequest(string body, string? accessToken = "token-1")
    {
        var httpContext = new DefaultHttpContext
        {
            Request = { Body = new MemoryStream(Encoding.UTF8.GetBytes(body)) },
        };
        if (accessToken is not null)
            httpContext.Request.Headers["asaas-access-token"] = accessToken;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task Receive_Success_ShouldForwardRawPayloadAndTokenAndReturnOk()
    {
        SetupRequest("""{"event":"PAYMENT_CONFIRMED"}""", "token-1");
        _mediator.Send(Arg.Is<ReceiveAsaasWebhookCommand>(c => c.RawPayload.Contains("PAYMENT_CONFIRMED") && c.AccessToken == "token-1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Receive(CancellationToken.None);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Receive_InvalidToken_ShouldReturnUnauthorized()
    {
        SetupRequest("{}", "bad-token");
        _mediator.Send(Arg.Any<ReceiveAsaasWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Asaas.InvalidWebhookToken", "token invalido")));

        var result = await _controller.Receive(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Receive_OtherFailure_ShouldStillReturnOk()
    {
        SetupRequest("{}");
        _mediator.Send(Arg.Any<ReceiveAsaasWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Asaas.ParseError", "payload invalido")));

        var result = await _controller.Receive(CancellationToken.None);

        result.Should().BeOfType<OkResult>();
    }
}
