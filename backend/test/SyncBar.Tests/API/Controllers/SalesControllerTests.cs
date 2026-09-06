using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Billing.GetSalesBySession;
using SyncBar.Application.Features.Billing.RefundSale;
using SyncBar.Application.Features.Billing.RegisterPartialPayment;
using SyncBar.Application.Features.Billing.RegisterSale;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class SalesControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly SalesController _controller;

    public SalesControllerTests()
    {
        _controller = new SalesController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    private static RegisterSaleCommand ValidRegisterCommand() => new(
        1, 1, 1, [new SalePaymentInput(1, 50m, null, null)]);

    [Fact]
    public async Task Register_Success_ShouldReturnOkWithValue()
    {
        var command = ValidRegisterCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(9L));

        var result = await _controller.Register(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(9L);
    }

    [Fact]
    public async Task Register_Failure_ShouldReturnMappedErrorResult()
    {
        var command = ValidRegisterCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.Register(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetBySession_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetSalesBySessionQuery>(q => q.CashSessionId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<SessionSaleResponse>>([]));

        var result = await _controller.GetBySession(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetBySession_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetSalesBySessionQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<SessionSaleResponse>>(new Error("CashSession.NotFound", "sessao nao encontrada")));

        var result = await _controller.GetBySession(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Refund_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new RefundSaleRequest(1, "cliente desistiu");
        _mediator.Send(Arg.Is<RefundSaleCommand>(c => c.SaleId == 1 && c.EmployeeId == 1 && c.Reason == "cliente desistiu"),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Refund(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Refund_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new RefundSaleRequest(1, null);
        _mediator.Send(Arg.Any<RefundSaleCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Sale.NotFound", "venda nao encontrada")));

        var result = await _controller.Refund(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static RegisterPartialPaymentCommand ValidPartialCommand() => new(1, 1, 1, 1, 25m, null, null);

    [Fact]
    public async Task RegisterPartial_Success_ShouldReturnOkWithValue()
    {
        var command = ValidPartialCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(3L));

        var result = await _controller.RegisterPartial(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(3L);
    }

    [Fact]
    public async Task RegisterPartial_Failure_ShouldReturnMappedErrorResult()
    {
        var command = ValidPartialCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.RegisterPartial(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
