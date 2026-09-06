using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Stock;
using SyncBar.Application.Features.Stock.AdjustInventory;
using SyncBar.Application.Features.Stock.GetByBranch;
using SyncBar.Application.Features.Stock.GetLedger;
using SyncBar.Application.Features.Stock.RegisterMovement;
using SyncBar.Application.Features.Stock.SetLimits;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class StockControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly StockController _controller;

    public StockControllerTests()
    {
        _controller = new StockController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetByBranch_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetStockByBranchQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<StockItemResponse>>([]));

        var result = await _controller.GetByBranch(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByBranch_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetStockByBranchQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<StockItemResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetByBranch(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetLedger_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetStockLedgerQuery>(q => q.StockItemId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<StockMovementResponse>>([]));

        var result = await _controller.GetLedger(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetLedger_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetStockLedgerQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<StockMovementResponse>>(new Error("StockItem.NotFound", "item de estoque nao encontrado")));

        var result = await _controller.GetLedger(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static RegisterStockMovementCommand ValidMovementCommand() => new(1, 1, 1, 1, 10m, 5m, null, null);

    [Fact]
    public async Task RegisterMovement_Success_ShouldReturnOkWithValue()
    {
        var command = ValidMovementCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(3L));

        var result = await _controller.RegisterMovement(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(3L);
    }

    [Fact]
    public async Task RegisterMovement_Failure_ShouldReturnMappedErrorResult()
    {
        var command = ValidMovementCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("Product.NotFound", "produto nao encontrado")));

        var result = await _controller.RegisterMovement(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static AdjustInventoryCommand ValidAdjustCommand() => new(1, 1, [new InventoryCountInput(1, 10m)]);

    [Fact]
    public async Task AdjustInventory_Success_ShouldReturnOkWithValue()
    {
        var command = ValidAdjustCommand();
        var response = new[] { new InventoryAdjustmentResponse(1, 8m, 10m, 2m) };
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success<IReadOnlyCollection<InventoryAdjustmentResponse>>(response));

        var result = await _controller.AdjustInventory(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task AdjustInventory_Failure_ShouldReturnMappedErrorResult()
    {
        var command = ValidAdjustCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<InventoryAdjustmentResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.AdjustInventory(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetLimits_Success_ShouldSendCommandWithIdAndReturnNoContent()
    {
        var request = new SetStockLimitsRequest(2m, 20m);
        _mediator.Send(Arg.Is<SetStockLimitsCommand>(c => c.StockItemId == 1 && c.MinimumQuantity == 2m && c.MaximumQuantity == 20m),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.SetLimits(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetLimits_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new SetStockLimitsRequest(2m, null);
        _mediator.Send(Arg.Any<SetStockLimitsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("StockItem.NotFound", "item de estoque nao encontrado")));

        var result = await _controller.SetLimits(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
