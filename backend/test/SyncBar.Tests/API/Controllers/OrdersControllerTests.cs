using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SyncBar.API.Controllers;
using SyncBar.Application.Features.Orders;
using SyncBar.Application.Features.Orders.AddItem;
using SyncBar.Application.Features.Orders.AddItemComplement;
using SyncBar.Application.Features.Orders.AddPizzaItem;
using SyncBar.Application.Features.Orders.ApplyDiscount;
using SyncBar.Application.Features.Orders.Cancel;
using SyncBar.Application.Features.Orders.Close;
using SyncBar.Application.Features.Orders.GetById;
using SyncBar.Application.Features.Orders.GetOpenByBranch;
using SyncBar.Application.Features.Orders.GetQrViewSetting;
using SyncBar.Application.Features.Orders.GetTableReadingValidationSetting;
using SyncBar.Application.Features.Orders.Open;
using SyncBar.Application.Features.Orders.RaiseComandaLimit;
using SyncBar.Application.Features.Orders.RemoveItemComplement;
using SyncBar.Application.Features.Orders.RemoveServiceFee;
using SyncBar.Application.Features.Orders.Reopen;
using SyncBar.Application.Features.Orders.ServiceFeeSetting;
using SyncBar.Application.Features.Orders.SetQrViewEnabled;
using SyncBar.Application.Features.Orders.SetTableReadingValidation;
using SyncBar.Application.Features.Orders.SplitBill;
using SyncBar.Application.Features.Orders.TransferComandaAllItem;
using SyncBar.Application.Features.Orders.TransferComandaItem;
using SyncBar.Application.Features.Orders.TransferItem;
using SyncBar.Application.Features.Orders.TransferTableItems;
using SyncBar.Application.Features.Orders.UpdateItemStatus;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
using SyncBar.Tests.API.Controllers.TestSupport;
using Xunit;

namespace SyncBar.Tests.API.Controllers;

public sealed class OrdersControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogTrackerRepository _logRepository = Substitute.For<ILogTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _controller = new OrdersController(_mediator, _logRepository, _unitOfWork);
        ControllerTestHelpers.AttachHttpContext(_controller);
    }

    [Fact]
    public async Task GetById_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetOrderByIdQuery>(q => q.CustomerOrderId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success((OrderResponse)null!));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetOrderByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<OrderResponse>(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.GetById(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetOpenByBranch_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetOpenOrdersByBranchQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<OrderResponse>>([]));

        var result = await _controller.GetOpenByBranch(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetOpenByBranch_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetOpenOrdersByBranchQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<OrderResponse>>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetOpenByBranch(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static OpenOrderCommand ValidOpenCommand() => new(1, 1, null, 1, 4, null);

    [Fact]
    public async Task Open_Success_ShouldReturnCreatedAtActionPointingToGetById()
    {
        var command = ValidOpenCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success(5L));

        var result = await _controller.Open(command, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(_controller.GetById));
        created.RouteValues!["id"].Should().Be(5L);
        created.Value.Should().Be(5L);
    }

    [Fact]
    public async Task Open_Failure_ShouldReturnMappedErrorResult()
    {
        var command = ValidOpenCommand();
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure<long>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.Open(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddItem_Success_ShouldSendCommandAndReturnNoContent()
    {
        var request = new AddOrderItemRequest(10, 2, "sem cebola", 1);
        _mediator.Send(Arg.Is<AddOrderItemCommand>(c => c.CustomerOrderId == 1 && c.ProductId == 10 && c.Quantity == 2),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.AddItem(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task AddItem_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new AddOrderItemRequest(10, 2, null, null);
        _mediator.Send(Arg.Any<AddOrderItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.AddItem(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddPizzaItem_Success_ShouldSendCommandAndReturnNoContent()
    {
        var request = new AddPizzaOrderItemRequest(10, 1, null, null, 1, null, null, [2, 3]);
        _mediator.Send(Arg.Is<AddPizzaOrderItemCommand>(c => c.CustomerOrderId == 1 && c.PizzaSizeId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.AddPizzaItem(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task AddPizzaItem_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new AddPizzaOrderItemRequest(10, 1, null, null, 1, null, null, [2, 3]);
        _mediator.Send(Arg.Any<AddPizzaOrderItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.AddPizzaItem(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddItemComplement_Success_ShouldSendCommandAndReturnNoContent()
    {
        var request = new AddOrderItemComplementRequest(5, 6, 1);
        _mediator.Send(Arg.Is<AddOrderItemComplementCommand>(c => c.CustomerOrderId == 1 && c.OrderItemId == 2 && c.ComplementGroupId == 5),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.AddItemComplement(1, 2, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task AddItemComplement_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new AddOrderItemComplementRequest(5, 6, null);
        _mediator.Send(Arg.Any<AddOrderItemComplementCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("OrderItem.NotFound", "item nao encontrado")));

        var result = await _controller.AddItemComplement(999, 999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RemoveItemComplement_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<RemoveOrderItemComplementCommand>(c => c.CustomerOrderId == 1 && c.OrderItemId == 2 && c.OrderItemComplementId == 3),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.RemoveItemComplement(1, 2, 3, 1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveItemComplement_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<RemoveOrderItemComplementCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("OrderItemComplement.NotFound", "complemento nao encontrado")));

        var result = await _controller.RemoveItemComplement(999, 999, 999, null, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateItemStatus_NonManagerUser_ShouldSendCommandWithIsManagerFalseAndReturnNoContent()
    {
        var request = new UpdateOrderItemStatusRequest(2, 10);
        _mediator.Send(Arg.Is<UpdateOrderItemStatusCommand>(c =>
                c.CustomerOrderId == 1 && c.OrderItemId == 3 && c.OrderItemStatusId == 2 && c.ActorEmployeeId == 10 && !c.IsManager),
            Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.UpdateItemStatus(1, 3, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateItemStatus_ManagerUser_ShouldSendCommandWithIsManagerTrue()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Gerente")], "TestAuth")),
        };
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        var request = new UpdateOrderItemStatusRequest(2, null);
        _mediator.Send(Arg.Any<UpdateOrderItemStatusCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        await _controller.UpdateItemStatus(1, 3, request, CancellationToken.None);

        await _mediator.Received(1).Send(Arg.Is<UpdateOrderItemStatusCommand>(c => c.IsManager), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateItemStatus_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new UpdateOrderItemStatusRequest(2, null);
        _mediator.Send(Arg.Any<UpdateOrderItemStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("OrderItem.NotFound", "item nao encontrado")));

        var result = await _controller.UpdateItemStatus(1, 3, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ApplyDiscount_Success_ShouldSendCommandAndReturnNoContent()
    {
        var request = new ApplyOrderDiscountRequest(10m);
        _mediator.Send(Arg.Is<ApplyOrderDiscountCommand>(c => c.CustomerOrderId == 1 && c.DiscountAmount == 10m), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.ApplyDiscount(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ApplyDiscount_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new ApplyOrderDiscountRequest(10m);
        _mediator.Send(Arg.Any<ApplyOrderDiscountCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.ApplyDiscount(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Close_Success_ShouldSendCommandAndReturnNoContent()
    {
        var request = new CloseOrderRequest(0.10m);
        _mediator.Send(Arg.Is<CloseOrderCommand>(c => c.CustomerOrderId == 1 && c.ServiceFeeRate == 0.10m), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Close(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Close_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new CloseOrderRequest();
        _mediator.Send(Arg.Any<CloseOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.Close(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Reopen_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<ReopenOrderCommand>(c => c.CustomerOrderId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Reopen(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Reopen_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<ReopenOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.Reopen(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RaiseCreditLimit_Success_ShouldSendCommandAndReturnNoContent()
    {
        var request = new RaiseCreditLimitRequest(200m);
        _mediator.Send(Arg.Is<RaiseComandaLimitCommand>(c => c.CustomerOrderId == 1 && c.NewLimitAmount == 200m), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.RaiseCreditLimit(1, request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RaiseCreditLimit_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new RaiseCreditLimitRequest(200m);
        _mediator.Send(Arg.Any<RaiseComandaLimitCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.RaiseCreditLimit(999, request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RemoveServiceFee_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<RemoveServiceFeeCommand>(c => c.CustomerOrderId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.RemoveServiceFee(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveServiceFee_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<RemoveServiceFeeCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.RemoveServiceFee(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetServiceFeeSetting_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetServiceFeeSettingQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ServiceFeeSettingResponse(true)));

        var result = await _controller.GetServiceFeeSetting(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetServiceFeeSetting_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetServiceFeeSettingQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ServiceFeeSettingResponse>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetServiceFeeSetting(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetServiceFeeEnabled_Success_ShouldSendCommandAndReturnNoContent()
    {
        var command = new SetServiceFeeEnabledCommand(1, true);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.SetServiceFeeEnabled(command, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetServiceFeeEnabled_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new SetServiceFeeEnabledCommand(999, true);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.SetServiceFeeEnabled(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetQrViewSetting_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetQrViewSettingQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new QrViewSettingResponse(true)));

        var result = await _controller.GetQrViewSetting(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetQrViewSetting_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetQrViewSettingQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<QrViewSettingResponse>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetQrViewSetting(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetQrViewEnabled_Success_ShouldSendCommandAndReturnNoContent()
    {
        var command = new SetQrViewEnabledCommand(1, true);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.SetQrViewEnabled(command, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetQrViewEnabled_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new SetQrViewEnabledCommand(999, true);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.SetQrViewEnabled(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetTableReadingValidationSetting_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<GetTableReadingValidationSettingQuery>(q => q.BranchId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new TableReadingValidationSettingResponse(true, false, true)));

        var result = await _controller.GetTableReadingValidationSetting(1, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTableReadingValidationSetting_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<GetTableReadingValidationSettingQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<TableReadingValidationSettingResponse>(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.GetTableReadingValidationSetting(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetTableReadingValidation_Success_ShouldSendCommandAndReturnNoContent()
    {
        var command = new SetTableReadingValidationCommand(1, true, false, true);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _controller.SetTableReadingValidation(command, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SetTableReadingValidation_Failure_ShouldReturnMappedErrorResult()
    {
        var command = new SetTableReadingValidationCommand(999, true, false, true);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Result.Failure(new Error("Branch.NotFound", "filial nao encontrada")));

        var result = await _controller.SetTableReadingValidation(command, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CalculateSplit_Success_ShouldSendQueryAndReturnOk()
    {
        _mediator.Send(Arg.Is<CalculateBillSplitQuery>(q => q.CustomerOrderId == 1 && q.PeopleCount == 2), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new BillSplitResponse(100m, 2, [new BillShareResponse(1, 50m), new BillShareResponse(2, 50m)])));

        var result = await _controller.CalculateSplit(1, 2, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CalculateSplit_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<CalculateBillSplitQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<BillSplitResponse>(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.CalculateSplit(999, 2, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Cancel_Success_ShouldSendCommandAndReturnNoContent()
    {
        _mediator.Send(Arg.Is<CancelOrderCommand>(c => c.CustomerOrderId == 1), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _controller.Cancel(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Cancel_Failure_ShouldReturnMappedErrorResult()
    {
        _mediator.Send(Arg.Any<CancelOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.Cancel(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task TransferComandaItems_Success_ShouldSendCommandAndReturnNoContent()
    {
        var request = new TransferComandaItemsRequest(1, 2, [10, 11], 3, 4, 5);
        _mediator.Send(Arg.Is<TransferComandaItemsCommand>(c => c.SourceCustomerOrderId == 1 && c.TargetCustomerOrderId == 2),
            Arg.Any<CancellationToken>()).Returns(Result.Success(Unit.Value));

        var result = await _controller.TransferComandaItems(request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task TransferComandaItems_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new TransferComandaItemsRequest(1, 2, [10, 11], 3, 4, 5);
        _mediator.Send(Arg.Any<TransferComandaItemsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<Unit>(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.TransferComandaItems(request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task TransferComandaItem_Success_ShouldSendCommandAndReturnNoContent()
    {
        var request = new TransferComandaItemRequest(1, 2, 10, 3, 4, 5);
        _mediator.Send(Arg.Is<TransferComandaItemCommand>(c => c.SourceCustomerOrderId == 1 && c.TargetCustomerOrderId == 2),
            Arg.Any<CancellationToken>()).Returns(Result.Success(99L));

        var result = await _controller.TransferComandaItem(request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task TransferComandaItem_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new TransferComandaItemRequest(1, 2, 10, 3, 4, 5);
        _mediator.Send(Arg.Any<TransferComandaItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<long>(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.TransferComandaItem(request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task TransferTableItems_Success_ShouldSendCommandAndReturnNoContent()
    {
        var request = new TransferTableItemsRequest(1, 2, [10, 11], 3, 4, 5);
        _mediator.Send(Arg.Is<TransferTableItemsCommand>(c => c.SourceCustomerOrderId == 1 && c.TargetCustomerOrderId == 2),
            Arg.Any<CancellationToken>()).Returns(Result.Success(Unit.Value));

        var result = await _controller.TransferTableItems(request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task TransferTableItems_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new TransferTableItemsRequest(1, 2, [10, 11], 3, 4, 5);
        _mediator.Send(Arg.Any<TransferTableItemsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<Unit>(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.TransferTableItems(request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task TransferItem_Success_ShouldSendCommandAndReturnNoContent()
    {
        var request = new TransferTableItemRequest(1, 2, 10, 3, 4, 5);
        _mediator.Send(Arg.Is<TransferTableItemCommand>(c => c.SourceCustomerOrderId == 1 && c.TargetCustomerOrderId == 2),
            Arg.Any<CancellationToken>()).Returns(Result.Success(99L));

        var result = await _controller.TransferItem(request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task TransferItem_Failure_ShouldReturnMappedErrorResult()
    {
        var request = new TransferTableItemRequest(1, 2, 10, 3, 4, 5);
        _mediator.Send(Arg.Any<TransferTableItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<long>(new Error("CustomerOrder.NotFound", "pedido nao encontrado")));

        var result = await _controller.TransferItem(request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
