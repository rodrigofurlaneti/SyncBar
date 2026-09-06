using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Orders.AddItem;
using SyncBar.Application.Features.PublicOrdering.AddItem;
using SyncBar.Application.Features.PublicOrdering.GetPublicBill;
using SyncBar.Application.Features.PublicOrdering.GetPublicComandaBill;
using SyncBar.Application.Features.PublicOrdering.GetPublicMenu;
using SyncBar.Application.Features.PublicOrdering.ValidateComandaReading;
using SyncBar.Application.Features.PublicOrdering.ValidateTableReading;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class PublicOrderingControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PublicOrderingController _controller;

    public PublicOrderingControllerTests()
    {
        _controller = new PublicOrderingController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetMenu_Success_ShouldSendQueryAndReturnOk()
    {
        var token = Guid.NewGuid();
        _mediator.Send(Arg.Is<GetPublicMenuQuery>(q => q.Token == token), Arg.Any<CancellationToken>())
            .Returns(Result.Success((PublicMenuResponse)null!));

        var result = await _controller.GetMenu(token, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMenu_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetPublicMenuQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PublicMenuResponse>(new Error("DiningTable.NotFound", "mesa nao encontrada")));

        var result = await _controller.GetMenu(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetBill_Success_ShouldSendQueryAndReturnOk()
    {
        var token = Guid.NewGuid();
        _mediator.Send(Arg.Is<GetPublicBillQuery>(q => q.Token == token), Arg.Any<CancellationToken>())
            .Returns(Result.Success((PublicBillResponse)null!));

        var result = await _controller.GetBill(token, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetBill_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetPublicBillQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PublicBillResponse>(new Error("DiningTable.NotFound", "mesa nao encontrada")));

        var result = await _controller.GetBill(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetComandaBill_Success_ShouldForwardTokenAndCodeAndReturnOk()
    {
        var token = Guid.NewGuid();
        _mediator.Send(Arg.Is<GetPublicComandaBillQuery>(q => q.TableToken == token && q.ComandaCode == "C1"), Arg.Any<CancellationToken>())
            .Returns(Result.Success((PublicComandaBillResponse)null!));

        var result = await _controller.GetComandaBill(token, "C1", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetComandaBill_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetPublicComandaBillQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PublicComandaBillResponse>(new Error("Comanda.NotFound", "comanda nao encontrada")));

        var result = await _controller.GetComandaBill(Guid.NewGuid(), "C1", CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddItem_Success_ShouldForwardAllFieldsAndReturnOkWithWrappedOrderId()
    {
        var token = Guid.NewGuid();
        var request = new AddPublicOrderItemRequest(1, 2, "sem cebola", [new OrderItemComplementSelection(1, 1)], "C1");
        _mediator.Send(Arg.Is<AddPublicOrderItemCommand>(c =>
                c.Token == token && c.ProductId == 1 && c.Quantity == 2 && c.Notes == "sem cebola"
                && c.Complements!.Count == 1 && c.ComandaCode == "C1"),
            Arg.Any<CancellationToken>()).Returns(Result.Success(55L));

        var result = await _controller.AddItem(token, request, CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new { orderId = 55L });
    }

    [Fact]
    public async Task AddItem_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new AddPublicOrderItemRequest(1, 1, null);
        _mediator.Send(Arg.Any<AddPublicOrderItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<long>(new Error("Product.NotFound", "produto nao encontrado")));

        var result = await _controller.AddItem(Guid.NewGuid(), request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ValidateComandaReading_Success_ShouldForwardAllFieldsAndReturnNoContent()
    {
        var token = Guid.NewGuid();
        var request = new ValidateComandaReadingRequest("qrcode", "scanned-value", null);
        _mediator.Send(Arg.Is<ValidateComandaReadingCommand>(c =>
                c.TableToken == token && c.ComandaCode == "C1" && c.Method == "qrcode" && c.ScannedValue == "scanned-value"),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.ValidateComandaReading(token, "C1", request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ValidateComandaReading_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new ValidateComandaReadingRequest("camera", null, "base64img");
        _mediator.Send(Arg.Any<ValidateComandaReadingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Comanda.NotFound", "comanda nao encontrada")));

        var result = await _controller.ValidateComandaReading(Guid.NewGuid(), "C1", request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ValidateTableReading_Success_ShouldForwardAllFieldsAndReturnNoContent()
    {
        var token = Guid.NewGuid();
        var request = new ValidateComandaReadingRequest("barcode", "12345", null);
        _mediator.Send(Arg.Is<ValidateTableReadingCommand>(c => c.TableToken == token && c.Method == "barcode" && c.ScannedValue == "12345"),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.ValidateTableReading(token, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ValidateTableReading_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new ValidateComandaReadingRequest("camera", null, "base64img");
        _mediator.Send(Arg.Any<ValidateTableReadingCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("DiningTable.NotFound", "mesa nao encontrada")));

        var result = await _controller.ValidateTableReading(Guid.NewGuid(), request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
