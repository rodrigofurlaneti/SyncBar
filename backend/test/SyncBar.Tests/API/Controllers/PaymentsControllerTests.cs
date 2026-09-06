using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Abstractions.Payments;
using SyncBar.Application.Features.Payments.ChargePayment;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class PaymentsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PaymentsController _controller;

    public PaymentsControllerTests()
    {
        _controller = new PaymentsController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task Charge_Success_ShouldReturnOkWithValue()
    {
        var command = new ChargePaymentCommand(1, 50m, PaymentGatewayMethod.Pix, null);
        var response = new ChargePaymentResponse("tx-1", "PENDING", "qr-code-payload");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(response));

        var result = await _controller.Charge(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task Charge_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new ChargePaymentCommand(1, 50m, PaymentGatewayMethod.CreditCard, null);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<ChargePaymentResponse>(new Error("Sale.NotFound", "venda nao encontrada")));

        var result = await _controller.Charge(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
