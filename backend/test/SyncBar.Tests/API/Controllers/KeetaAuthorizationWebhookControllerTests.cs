using System.Text;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Integrations.Keeta.Authorization.ProcessAuthorizationWebhook;
using SyncBar.Domain.Primitives;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class KeetaAuthorizationWebhookControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly KeetaAuthorizationWebhookController _controller;

    public KeetaAuthorizationWebhookControllerTests()
    {
        _controller = new KeetaAuthorizationWebhookController(_mediator);
    }

    private void SetupRequest(string body)
    {
        var httpContext = new DefaultHttpContext
        {
            Request = { Body = new MemoryStream(Encoding.UTF8.GetBytes(body)) },
        };
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task Receive_Success_ShouldForwardPayloadAndSignatureAndReturnOk()
    {
        SetupRequest("""{"opType":1}""");
        _mediator.Send(Arg.Is<ProcessKeetaAuthorizationWebhookCommand>(c => c.RawPayload.Contains("opType") && c.Signature == "sig-1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Receive("sig-1", CancellationToken.None);

        result.Should().BeOfType<OkResult>();
    }

    [Theory]
    [InlineData("Keeta.InvalidSignature")]
    [InlineData("Keeta.MissingSignature")]
    public async Task Receive_SignatureFailure_ShouldReturnUnauthorized(string errorCode)
    {
        SetupRequest("{}");
        _mediator.Send(Arg.Any<ProcessKeetaAuthorizationWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error(errorCode, "assinatura invalida")));

        var result = await _controller.Receive(null, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Receive_InvalidPayload_ShouldReturnBadRequest()
    {
        SetupRequest("not-json");
        _mediator.Send(Arg.Any<ProcessKeetaAuthorizationWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Keeta.InvalidPayload", "payload invalido")));

        var result = await _controller.Receive("sig-1", CancellationToken.None);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task Receive_OtherFailure_ShouldStillReturnOk()
    {
        SetupRequest("{}");
        _mediator.Send(Arg.Any<ProcessKeetaAuthorizationWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Keeta.Unknown", "erro desconhecido")));

        var result = await _controller.Receive("sig-1", CancellationToken.None);

        result.Should().BeOfType<OkResult>();
    }
}
