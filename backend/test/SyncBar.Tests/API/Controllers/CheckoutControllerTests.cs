using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Checkout.PayOrderWithBoleto;
using SyncBar.Application.Features.Checkout.PayOrderWithCreditCard;
using SyncBar.Application.Features.Checkout.PayOrderWithPix;
using SyncBar.Application.Features.Integrations.Asaas.Payment.Create;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class CheckoutControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CheckoutController _controller;

    public CheckoutControllerTests()
    {
        _controller = new CheckoutController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task PayWithPix_Success_ShouldSendCommandAndReturnOk()
    {
        var response = new PayOrderWithPixResponse(1, "asaas-1", "PENDING", "base64img", "copia-cola", "https://x", 50m);
        _mediator.Send(Arg.Is<PayOrderWithPixCommand>(c => c.CustomerOrderId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success(response));

        var result = await _controller.PayWithPix(new CheckoutPixRequest(1), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task PayWithPix_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<PayOrderWithPixCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PayOrderWithPixResponse>(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.PayWithPix(new CheckoutPixRequest(999), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task PayWithCreditCard_Success_ShouldForwardAllFieldsAndReturnOk()
    {
        var card = new CreditCardDataRequest("Joao Silva", "4111111111111111", "12", "2030", "123");
        var response = new PayOrderWithCreditCardResponse(1, "asaas-1", "CONFIRMED", "VISA", "1111", 50m);
        _mediator.Send(Arg.Is<PayOrderWithCreditCardCommand>(c =>
                c.CustomerOrderId == 1 && c.SavedCardId == null && c.Card == card && c.SaveCard),
            Arg.Any<CancellationToken>()).Returns(Result.Success(response));

        var result = await _controller.PayWithCreditCard(new CheckoutCreditCardRequest(1, null, card, true), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task PayWithCreditCard_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<PayOrderWithCreditCardCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PayOrderWithCreditCardResponse>(new Error("Payment.Declined", "pagamento recusado")));

        var result = await _controller.PayWithCreditCard(new CheckoutCreditCardRequest(1, 1, null), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task PayWithBoleto_Success_ShouldSendCommandAndReturnOk()
    {
        var response = new PayOrderWithBoletoResponse(1, "asaas-1", "PENDING", "https://boleto", "341...", "34199...", 50m, DateTime.Today.AddDays(3));
        _mediator.Send(Arg.Is<PayOrderWithBoletoCommand>(c => c.CustomerOrderId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success(response));

        var result = await _controller.PayWithBoleto(new CheckoutBoletoRequest(1), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task PayWithBoleto_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<PayOrderWithBoletoCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PayOrderWithBoletoResponse>(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.PayWithBoleto(new CheckoutBoletoRequest(999), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
