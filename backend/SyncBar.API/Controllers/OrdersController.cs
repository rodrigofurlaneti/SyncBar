using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Orders.AddItem;
using SyncBar.Application.Features.Orders.ApplyDiscount;
using SyncBar.Application.Features.Orders.Cancel;
using SyncBar.Application.Features.Orders.Close;
using SyncBar.Application.Features.Orders.GetById;
using SyncBar.Application.Features.Orders.GetOpenByBranch;
using SyncBar.Application.Features.Orders.Open;
using SyncBar.Application.Features.Orders.UpdateItemStatus;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Salao")]
public sealed class OrdersController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetOrderByIdQuery(id), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpGet("open/branch/{branchId:long}")]
    public async Task<IActionResult> GetOpenByBranch(long branchId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetOpenOrdersByBranchQuery(branchId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Open([FromBody] OpenOrderCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure
            ? HandleFailure(result)
            : CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
    }

    [HttpPost("{id:long}/items")]
    public async Task<IActionResult> AddItem(long id, [FromBody] AddOrderItemRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(
            new AddOrderItemCommand(id, request.ProductId, request.Quantity, request.Notes, request.EmployeeId), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    [HttpPut("{id:long}/items/{itemId:long}/status")]
    public async Task<IActionResult> UpdateItemStatus(long id, long itemId, [FromBody] UpdateOrderItemStatusRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new UpdateOrderItemStatusCommand(id, itemId, request.OrderItemStatusId), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    [HttpPut("{id:long}/discount")]
    public async Task<IActionResult> ApplyDiscount(long id, [FromBody] ApplyOrderDiscountRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new ApplyOrderDiscountCommand(id, request.DiscountAmount), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    [HttpPut("{id:long}/close")]
    public async Task<IActionResult> Close(long id, [FromBody] CloseOrderRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new CloseOrderCommand(id, request.ServiceFeeRate), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    [HttpPut("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        var result = await Mediator.Send(new CancelOrderCommand(id), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
}

// Requests separados dos commands quando ha parametro de rota.
public sealed record AddOrderItemRequest(long ProductId, decimal Quantity, string? Notes, long? EmployeeId);
public sealed record UpdateOrderItemStatusRequest(long OrderItemStatusId);
public sealed record ApplyOrderDiscountRequest(decimal DiscountAmount);
public sealed record CloseOrderRequest(decimal ServiceFeeRate = 0.10m);
