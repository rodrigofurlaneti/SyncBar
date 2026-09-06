using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Application.Features.Printing;
using SyncBar.Application.Features.Printing.CreatePrinter;
using SyncBar.Application.Features.Printing.DeactivatePrinter;
using SyncBar.Application.Features.Printing.GetPrinters;
using SyncBar.Application.Features.Printing.SetSettings;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class PrintersControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IPrintingService _printingService = Substitute.For<IPrintingService>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PrintersController _controller;

    public PrintersControllerTests()
    {
        _controller = new PrintersController(_mediator, _printingService, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetByBranch_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetPrintersQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<PrinterResponse>>([]));

        var result = await _controller.GetByBranch(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByBranch_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetPrintersQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<PrinterResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetByBranch(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static CreatePrinterCommand ValidCreateCommand() =>
        new(1, "Impressora Cozinha", 1, "EPSON", "192.168.0.10", 9100, true, false);

    [Fact]
    public async Task Create_Success_ShouldReturnOkWithId()
    {
        var command = ValidCreateCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(5L));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(5L);
    }

    [Fact]
    public async Task Create_Failure_ShouldReturnMappedErrorResult()
    {
        var command = ValidCreateCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Deactivate_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<DeactivatePrinterCommand>(c => c.PrinterId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Deactivate(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Deactivate_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<DeactivatePrinterCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Printer.NotFound", "impressora nao encontrada")));

        var result = await _controller.Deactivate(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetSettings_Success_ShouldSendCommandAndReturnNoContent()
    {
        var command = new SetPrintSettingsCommand(1, true, false);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.SetSettings(command, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetSettings_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new SetPrintSettingsCommand(999, true, false);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.SetSettings(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Test_Success_ShouldCallPrintingServiceAndReturnNoContent()
    {
        _printingService.PrintTestAsync(1, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.Test(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Test_Failure_ShouldReturnMappedErrorResult()
    {
        _printingService.PrintTestAsync(999, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Printer.NotFound", "impressora nao encontrada")));

        var result = await _controller.Test(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
