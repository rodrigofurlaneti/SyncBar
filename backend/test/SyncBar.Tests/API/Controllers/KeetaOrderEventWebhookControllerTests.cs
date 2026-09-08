using System.Text;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Integrations.Keeta.Order.Polling;
using SyncBar.Domain.Primitives;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class KeetaOrderEventWebhookControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly KeetaOrderEventWebhookController _controller;

    public KeetaOrderEventWebhookControllerTests()
    {
        _controller = new KeetaOrderEventWebhookController(_mediator);
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
    public async Task Receive_Success_ShouldForwardParsedMerchantIdAndReturnNoContent()
    {
        SetupRequest("""{"eventId":"evt-1"}""");
        _mediator.Send(Arg.Is<ProcessKeetaNewEventWebhookCommand>(c =>
                c.RawPayload.Contains("evt-1") && c.AppId == "app-1" && c.KeetaMerchantId == 123 && c.Signature == "sig-1"),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Receive("app-1", "123", "sig-1", CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Receive_NonNumericMerchantIdHeader_ShouldSendNullMerchantId()
    {
        SetupRequest("{}");
        _mediator.Send(Arg.Any<ProcessKeetaNewEventWebhookCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        await _controller.Receive("app-1", "not-a-number", "sig-1", CancellationToken.None);

        await _mediator.Received(1).Send(Arg.Is<ProcessKeetaNewEventWebhookCommand>(c => c.KeetaMerchantId == null), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("Keeta.InvalidSignature")]
    [InlineData("Keeta.MissingSignature")]
    public async Task Receive_SignatureFailure_ShouldReturnUnauthorized(string errorCode)
    {
        SetupRequest("{}");
        _mediator.Send(Arg.Any<ProcessKeetaNewEventWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error(errorCode, "assinatura invalida")));

        var result = await _controller.Receive("app-1", "123", "sig-1", CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Theory]
    [InlineData("Keeta.InvalidPayload")]
    [InlineData("Keeta.UnknownMerchant")]
    public async Task Receive_PayloadOrMerchantFailure_ShouldReturnBadRequest(string errorCode)
    {
        SetupRequest("{}");
        _mediator.Send(Arg.Any<ProcessKeetaNewEventWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error(errorCode, "erro")));

        var result = await _controller.Receive("app-1", "123", "sig-1", CancellationToken.None);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task Receive_OtherFailure_ShouldReturnServiceUnavailableForRetry()
    {
        SetupRequest("{}");
        _mediator.Send(Arg.Any<ProcessKeetaNewEventWebhookCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Keeta.Unknown", "erro desconhecido")));

        var result = await _controller.Receive("app-1", "123", "sig-1", CancellationToken.None);

        result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(503);
    }
}
