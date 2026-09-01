using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Orders.AddItem;
using SyncBar.Application.Features.Orders.AddItemComplement;
using SyncBar.Application.Features.Orders.AddPizzaItem;
using SyncBar.Application.Features.Orders.ApplyDiscount;
using SyncBar.Application.Features.Orders.Cancel;
using SyncBar.Application.Features.Orders.Close;
using SyncBar.Application.Features.Orders.GetById;
using SyncBar.Application.Features.Orders.GetOpenByBranch;
using SyncBar.Application.Features.Orders.Open;
using SyncBar.Application.Features.Orders.RaiseComandaLimit;
using SyncBar.Application.Features.Orders.RemoveItemComplement;
using SyncBar.Application.Features.Orders.RemoveServiceFee;
using SyncBar.Application.Features.Orders.Reopen;
using SyncBar.Application.Features.Orders.ServiceFeeSetting;
using SyncBar.Application.Features.Orders.SplitBill;
using SyncBar.Application.Features.Orders.TransferComandaItem;
using SyncBar.Application.Features.Orders.TransferItem;
using SyncBar.Application.Features.Orders.UpdateItemStatus;
using SyncBar.Application.Features.Orders.GetQrViewSetting;
using SyncBar.Application.Features.Orders.SetQrViewEnabled;
using SyncBar.Application.Features.Orders.GetTableReadingValidationSetting;
using SyncBar.Application.Features.Orders.SetTableReadingValidation;
using SyncBar.Domain.Repositories;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace SyncBar.API.Controllers;

[Authorize(Roles = "Administrador,Gerente")]
public sealed class OrdersController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("{id:long}")]
    public Task<IActionResult> GetById(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(GetById), async () =>
        {
            var result = await Mediator.Send(new GetOrderByIdQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("open/branch/{branchId:long}")]
    public Task<IActionResult> GetOpenByBranch(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(GetOpenByBranch), async () =>
        {
            var result = await Mediator.Send(new GetOpenOrdersByBranchQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost]
    public Task<IActionResult> Open([FromBody] OpenOrderCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(Open), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure
                ? HandleFailure(result)
                : CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
        });

    [HttpPost("{id:long}/items")]
    public Task<IActionResult> AddItem(long id, [FromBody] AddOrderItemRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(AddItem), async () =>
        {
            var result = await Mediator.Send(
                new AddOrderItemCommand(id, request.ProductId, request.Quantity, request.Notes, request.EmployeeId, request.Complements), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("{id:long}/pizza-items")]
    public Task<IActionResult> AddPizzaItem(long id, [FromBody] AddPizzaOrderItemRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(AddPizzaItem), async () =>
        {
            var result = await Mediator.Send(new AddPizzaOrderItemCommand(
                id, request.ProductId, request.Quantity, request.Notes, request.EmployeeId,
                request.PizzaSizeId, request.PizzaCrustId, request.PizzaEdgeId, request.PizzaFlavorIds), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("{id:long}/items/{itemId:long}/complements")]
    public Task<IActionResult> AddItemComplement(long id, long itemId, [FromBody] AddOrderItemComplementRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(AddItemComplement), async () =>
        {
            var result = await Mediator.Send(new AddOrderItemComplementCommand(
                id, itemId, request.ComplementGroupId, request.ComplementId, request.EmployeeId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpDelete("{id:long}/items/{itemId:long}/complements/{orderItemComplementId:long}")]
    public Task<IActionResult> RemoveItemComplement(long id, long itemId, long orderItemComplementId, [FromQuery] long? employeeId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(RemoveItemComplement), async () =>
        {
            var result = await Mediator.Send(new RemoveOrderItemComplementCommand(id, itemId, orderItemComplementId, employeeId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("{id:long}/items/{itemId:long}/status")]
    public Task<IActionResult> UpdateItemStatus(long id, long itemId, [FromBody] UpdateOrderItemStatusRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(UpdateItemStatus), async () =>
        {
            var isManager = User.IsInRole("Administrador") || User.IsInRole("Gerente");
            var result = await Mediator.Send(new UpdateOrderItemStatusCommand(
                id, itemId, request.OrderItemStatusId, request.ActorEmployeeId, isManager), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("{id:long}/discount")]
    public Task<IActionResult> ApplyDiscount(long id, [FromBody] ApplyOrderDiscountRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(ApplyDiscount), async () =>
        {
            var result = await Mediator.Send(new ApplyOrderDiscountCommand(id, request.DiscountAmount), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("{id:long}/close")]
    public Task<IActionResult> Close(long id, [FromBody] CloseOrderRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(Close), async () =>
        {
            var result = await Mediator.Send(new CloseOrderCommand(id, request.ServiceFeeRate), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("{id:long}/reopen")]
    public Task<IActionResult> Reopen(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(Reopen), async () =>
        {
            var result = await Mediator.Send(new ReopenOrderCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("{id:long}/credit-limit")]
    public Task<IActionResult> RaiseCreditLimit(long id, [FromBody] RaiseCreditLimitRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(RaiseCreditLimit), async () =>
        {
            var result = await Mediator.Send(new RaiseComandaLimitCommand(id, request.NewLimitAmount), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("{id:long}/remove-service-fee")]
    public Task<IActionResult> RemoveServiceFee(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(RemoveServiceFee), async () =>
        {
            var result = await Mediator.Send(new RemoveServiceFeeCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("service-fee-setting/branch/{branchId:long}")]
    public Task<IActionResult> GetServiceFeeSetting(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(GetServiceFeeSetting), async () =>
        {
            var result = await Mediator.Send(new GetServiceFeeSettingQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("service-fee-setting")]
    public Task<IActionResult> SetServiceFeeEnabled([FromBody] SetServiceFeeEnabledCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(SetServiceFeeEnabled), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("qr-view-setting/branch/{branchId:long}")]
    public Task<IActionResult> GetQrViewSetting(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(GetQrViewSetting), async () =>
        {
            var result = await Mediator.Send(new GetQrViewSettingQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("qr-view-setting")]
    public Task<IActionResult> SetQrViewEnabled([FromBody] SetQrViewEnabledCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(SetQrViewEnabled), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("table-reading-validation-setting/branch/{branchId:long}")]
    public Task<IActionResult> GetTableReadingValidationSetting(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(GetTableReadingValidationSetting), async () =>
        {
            var result = await Mediator.Send(new GetTableReadingValidationSettingQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("table-reading-validation-setting")]
    public Task<IActionResult> SetTableReadingValidation([FromBody] SetTableReadingValidationCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(SetTableReadingValidation), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpGet("{id:long}/split/{peopleCount:int}")]
    public Task<IActionResult> CalculateSplit(long id, int peopleCount, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(CalculateSplit), async () =>
        {
            var result = await Mediator.Send(new CalculateBillSplitQuery(id, peopleCount), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("{id:long}/cancel")]
    public Task<IActionResult> Cancel(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(Cancel), async () =>
        {
            var result = await Mediator.Send(new CancelOrderCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("comanda-items/transfer")]
    public Task<IActionResult> TransferComandaItem([FromBody] TransferComandaItemRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(TransferComandaItem), async () =>
        {
            var result = await Mediator.Send(new TransferComandaItemCommand(
                request.SourceCustomerOrderId,
                request.TargetCustomerOrderId,
                request.CustomerOrderItemId,
                request.SourceComandaId,
                request.TargetComandaId,
                request.ActorEmployeeId
            ), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("items/transfer")]
    public Task<IActionResult> TransferItem([FromBody] TransferTableItemRequest request, CancellationToken ct) =>
            ExecuteWithLogAsync(logRepository, unitOfWork, nameof(OrdersController), nameof(TransferItem), async () =>
            {
                var result = await Mediator.Send(new TransferTableItemCommand(
                    request.SourceCustomerOrderId,
                    request.TargetCustomerOrderId,
                    request.CustomerOrderItemId,
                    request.SourceDiningTableId,
                    request.TargetDiningTableId,
                    request.ActorEmployeeId
                ), ct);
                return result.IsFailure ? HandleFailure(result) : NoContent();
            });
}

// RECORDS
public sealed record TransferComandaItemRequest(
    [property: JsonRequired] long SourceCustomerOrderId,
    [property: JsonRequired] long TargetCustomerOrderId,
    [property: JsonRequired] long CustomerOrderItemId,
    [property: JsonRequired] long SourceComandaId,
    [property: JsonRequired] long TargetComandaId,
    [property: JsonRequired] long ActorEmployeeId
);

public sealed record AddOrderItemRequest(
    [property: JsonRequired] long ProductId,
    [property: JsonRequired] decimal Quantity,
    string? Notes,
    long? EmployeeId,
    IReadOnlyCollection<OrderItemComplementSelection>? Complements = null);

public sealed record AddOrderItemComplementRequest(
    [property: JsonRequired] long ComplementGroupId,
    [property: JsonRequired] long ComplementId,
    long? EmployeeId);

public sealed record AddPizzaOrderItemRequest(
    [property: JsonRequired] long ProductId,
    [property: JsonRequired] decimal Quantity,
    string? Notes,
    long? EmployeeId,
    [property: JsonRequired] long PizzaSizeId,
    long? PizzaCrustId,
    long? PizzaEdgeId,
    IReadOnlyCollection<long> PizzaFlavorIds);

public sealed record RaiseCreditLimitRequest([property: JsonRequired] decimal NewLimitAmount);

public sealed record UpdateOrderItemStatusRequest(
    [property: JsonRequired] long OrderItemStatusId, long? ActorEmployeeId = null);

public sealed record ApplyOrderDiscountRequest([property: JsonRequired] decimal DiscountAmount);

public sealed record CloseOrderRequest(decimal ServiceFeeRate = 0.10m);

public sealed record TransferTableItemRequest(
    [property: JsonRequired] long SourceCustomerOrderId,
    [property: JsonRequired] long TargetCustomerOrderId,
    [property: JsonRequired] long CustomerOrderItemId,
    [property: JsonRequired] long SourceDiningTableId,
    [property: JsonRequired] long TargetDiningTableId,
    [property: JsonRequired] long ActorEmployeeId
);