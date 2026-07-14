using System.Security.Claims;
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
using SyncBar.Application.Features.Orders.RaiseComandaLimit;
using SyncBar.Application.Features.Orders.Reopen;
using SyncBar.Application.Features.Orders.RemoveServiceFee;
using SyncBar.Application.Features.Orders.ServiceFeeSetting;
using SyncBar.Application.Features.Orders.SplitBill;
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
        var isManager = User.IsInRole("Administrador") || User.IsInRole("Gerente");
        var result = await Mediator.Send(new UpdateOrderItemStatusCommand(
            id, itemId, request.OrderItemStatusId, request.ActorEmployeeId, isManager), ct);
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

    // Fechou a conta por engano: reabre para consumo.
    [HttpPut("{id:long}/reopen")]
    public async Task<IActionResult> Reopen(long id, CancellationToken ct)
    {
        var result = await Mediator.Send(new ReopenOrderCommand(id), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    // Somente o gerente libera mais limite de comanda.
    [Authorize(Roles = "Administrador,Gerente")]
    [HttpPut("{id:long}/credit-limit")]
    public async Task<IActionResult> RaiseCreditLimit(long id, [FromBody] RaiseCreditLimitRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new RaiseComandaLimitCommand(id, request.NewLimitAmount), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    // Somente o gerente pode retirar os 10% — role exigida ALEM da policy do controller.
    [Authorize(Roles = "Administrador,Gerente")]
    [HttpPut("{id:long}/remove-service-fee")]
    public async Task<IActionResult> RemoveServiceFee(long id, CancellationToken ct)
    {
        var result = await Mediator.Send(new RemoveServiceFeeCommand(id), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    // Config da taxa de servico (10%) por filial — leitura liberada a quem ve o Salao.
    [HttpGet("service-fee-setting/branch/{branchId:long}")]
    public async Task<IActionResult> GetServiceFeeSetting(long branchId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetServiceFeeSettingQuery(branchId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    // Ligar/desligar os 10% — somente o gerente.
    [Authorize(Roles = "Administrador,Gerente")]
    [HttpPut("service-fee-setting")]
    public async Task<IActionResult> SetServiceFeeEnabled([FromBody] SetServiceFeeEnabledCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    // Divide a conta em N partes iguais (em centavos, sem perder nem sobrar 1 centavo) —
    // o caixa registra cada parte como um pagamento na mesma venda (RegisterSaleCommand.Payments).
    [HttpGet("{id:long}/split/{peopleCount:int}")]
    public async Task<IActionResult> CalculateSplit(long id, int peopleCount, CancellationToken ct)
    {
        var result = await Mediator.Send(new CalculateBillSplitQuery(id, peopleCount), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
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
public sealed record RaiseCreditLimitRequest(decimal NewLimitAmount);
public sealed record UpdateOrderItemStatusRequest(long OrderItemStatusId, long? ActorEmployeeId = null);
public sealed record ApplyOrderDiscountRequest(decimal DiscountAmount);
public sealed record CloseOrderRequest(decimal ServiceFeeRate = 0.10m);
