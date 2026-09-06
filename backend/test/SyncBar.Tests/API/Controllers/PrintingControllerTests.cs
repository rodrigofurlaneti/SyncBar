using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Application.Features.Printing;
using SyncBar.Application.Features.Printing.GetSettings;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class PrintingControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IPrintingService _printingService = Substitute.For<IPrintingService>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PrintingController _controller;

    public PrintingControllerTests()
    {
        _controller = new PrintingController(_mediator, _printingService, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetSettings_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetPrintSettingsQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PrintSettingsResponse(true, false)));

        var result = await _controller.GetSettings(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSettings_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetPrintSettingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PrintSettingsResponse>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetSettings(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task PrintBill_Success_ShouldCallPrintingServiceAndReturnNoContent()
    {
        _printingService.PrintBillAsync(1, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.PrintBill(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task PrintBill_Failure_ShouldReturnMappedErrorResult()
    {
        _printingService.PrintBillAsync(999, Arg.Any<CancellationToken>()).Returns(Result.Failure(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.PrintBill(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task PrintReceipt_Success_ShouldCallPrintingServiceAndReturnNoContent()
    {
        _printingService.PrintPaymentReceiptAsync(1, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.PrintReceipt(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task PrintReceipt_Failure_ShouldReturnMappedErrorResult()
    {
        _printingService.PrintPaymentReceiptAsync(999, Arg.Any<CancellationToken>()).Returns(Result.Failure(new Error("Sale.NotFound", "venda nao encontrada")));

        var result = await _controller.PrintReceipt(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task PrintPartialReceipt_Success_ShouldCallPrintingServiceAndReturnNoContent()
    {
        _printingService.PrintPartialReceiptAsync(1, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.PrintPartialReceipt(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task PrintPartialReceipt_Failure_ShouldReturnMappedErrorResult()
    {
        _printingService.PrintPartialReceiptAsync(999, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("OrderPartialPayment.NotFound", "pagamento nao encontrado")));

        var result = await _controller.PrintPartialReceipt(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task PrintCashClosing_Success_ShouldCallPrintingServiceAndReturnNoContent()
    {
        _printingService.PrintCashClosingAsync(1, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.PrintCashClosing(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task PrintCashClosing_Failure_ShouldReturnMappedErrorResult()
    {
        _printingService.PrintCashClosingAsync(999, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("CashSession.NotFound", "sessao nao encontrada")));

        var result = await _controller.PrintCashClosing(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
