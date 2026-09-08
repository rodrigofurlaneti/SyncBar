using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class IfoodWebhookReceiverControllerTests
{
    private readonly IIfoodWebhookReceiver _receiver = Substitute.For<IIfoodWebhookReceiver>();

    private IfoodWebhookReceiverController Create(string body)
    {
        var controller = new IfoodWebhookReceiverController(_receiver, NullLogger<IfoodWebhookReceiverController>.Instance)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        controller.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        controller.Request.Headers["X-IFood-Signature"] = "signature";
        return controller;
    }

    [Fact]
    public async Task Receive_ForwardsExactBytesAndReturnsAccepted()
    {
        const string body = "{\n \"description\": \"ação\" }";
        _receiver.ReceiveAsync(1, Arg.Is<byte[]>(value => value.SequenceEqual(Encoding.UTF8.GetBytes(body))), "signature", Arg.Any<CancellationToken>())
            .Returns(new IfoodWebhookReceipt(202));
        var result = await Create(body).Receive(1, default);
        result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(202);
    }

    [Fact]
    public async Task Receive_DatabaseFailureDoesNotReturnSuccess()
    {
        _receiver.ReceiveAsync(1, Arg.Any<byte[]>(), "signature", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IfoodWebhookReceipt>(new InvalidOperationException("Database unavailable")));
        var result = await Create("{}").Receive(1, default);
        result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task Receive_RejectsOversizedChunkedBodyBeforeProcessing()
    {
        var result = await Create(new string('x', 1_048_577)).Receive(1, default);
        result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(413);
        await _receiver.DidNotReceiveWithAnyArgs().ReceiveAsync(default, default!, default, default);
    }
}
