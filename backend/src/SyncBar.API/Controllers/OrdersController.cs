using System.Diagnostics;
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
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Salao")]
public sealed class OrdersController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("{id:long}")]
    public Task<IActionResult> GetById(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetById), async () =>
        {
            var result = await Mediator.Send(new GetOrderByIdQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("open/branch/{branchId:long}")]
    public Task<IActionResult> GetOpenByBranch(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetOpenByBranch), async () =>
        {
            var result = await Mediator.Send(new GetOpenOrdersByBranchQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost]
    public Task<IActionResult> Open([FromBody] OpenOrderCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(Open), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure
                ? HandleFailure(result)
                : CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
        });

    [HttpPost("{id:long}/items")]
    public Task<IActionResult> AddItem(long id, [FromBody] AddOrderItemRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(AddItem), async () =>
        {
            var result = await Mediator.Send(
                new AddOrderItemCommand(id, request.ProductId, request.Quantity, request.Notes, request.EmployeeId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("{id:long}/items/{itemId:long}/status")]
    public Task<IActionResult> UpdateItemStatus(long id, long itemId, [FromBody] UpdateOrderItemStatusRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(UpdateItemStatus), async () =>
        {
            var isManager = User.IsInRole("Administrador") || User.IsInRole("Gerente");
            var result = await Mediator.Send(new UpdateOrderItemStatusCommand(
                id, itemId, request.OrderItemStatusId, request.ActorEmployeeId, isManager), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("{id:long}/discount")]
    public Task<IActionResult> ApplyDiscount(long id, [FromBody] ApplyOrderDiscountRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(ApplyDiscount), async () =>
        {
            var result = await Mediator.Send(new ApplyOrderDiscountCommand(id, request.DiscountAmount), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("{id:long}/close")]
    public Task<IActionResult> Close(long id, [FromBody] CloseOrderRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(Close), async () =>
        {
            var result = await Mediator.Send(new CloseOrderCommand(id, request.ServiceFeeRate), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Fechou a conta por engano: reabre para consumo.
    [HttpPut("{id:long}/reopen")]
    public Task<IActionResult> Reopen(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(Reopen), async () =>
        {
            var result = await Mediator.Send(new ReopenOrderCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Somente o gerente libera mais limite de comanda.
    [Authorize(Roles = "Administrador,Gerente")]
    [HttpPut("{id:long}/credit-limit")]
    public Task<IActionResult> RaiseCreditLimit(long id, [FromBody] RaiseCreditLimitRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(RaiseCreditLimit), async () =>
        {
            var result = await Mediator.Send(new RaiseComandaLimitCommand(id, request.NewLimitAmount), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Somente o gerente pode retirar os 10% — role exigida ALEM da policy do controller.
    [Authorize(Roles = "Administrador,Gerente")]
    [HttpPut("{id:long}/remove-service-fee")]
    public Task<IActionResult> RemoveServiceFee(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(RemoveServiceFee), async () =>
        {
            var result = await Mediator.Send(new RemoveServiceFeeCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Config da taxa de servico (10%) por filial — leitura liberada a quem ve o Salao.
    [HttpGet("service-fee-setting/branch/{branchId:long}")]
    public Task<IActionResult> GetServiceFeeSetting(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetServiceFeeSetting), async () =>
        {
            var result = await Mediator.Send(new GetServiceFeeSettingQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Ligar/desligar os 10% — somente o gerente.
    [Authorize(Roles = "Administrador,Gerente")]
    [HttpPut("service-fee-setting")]
    public Task<IActionResult> SetServiceFeeEnabled([FromBody] SetServiceFeeEnabledCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(SetServiceFeeEnabled), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Divide a conta em N partes iguais (em centavos, sem perder nem sobrar 1 centavo) —
    // o caixa registra cada parte como um pagamento na mesma venda (RegisterSaleCommand.Payments).
    [HttpGet("{id:long}/split/{peopleCount:int}")]
    public Task<IActionResult> CalculateSplit(long id, int peopleCount, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(CalculateSplit), async () =>
        {
            var result = await Mediator.Send(new CalculateBillSplitQuery(id, peopleCount), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("{id:long}/cancel")]
    public Task<IActionResult> Cancel(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(Cancel), async () =>
        {
            var result = await Mediator.Send(new CancelOrderCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // --- WRAPPER DE LOG ---
    private async Task<IActionResult> ExecuteWithLogAsync(string methodName, Func<Task<IActionResult>> action)
    {
        var stopwatch = Stopwatch.StartNew();

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        long? appUserId = long.TryParse(userIdClaim, out var id) ? id : null;

        var log = new LogTracker(0)
        {
            AppUserId = appUserId,
            DirectoryName = "Controllers",
            ClassName = nameof(OrdersController),
            MethodName = methodName,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        try
        {
            var result = await action();

            if (result is OkObjectResult or NoContentResult or CreatedAtActionResult)
            {
                log.IsSuccess = true;
                log.Message = "Executado com sucesso.";
            }
            else
            {
                log.IsSuccess = false;
                log.Message = "Falha na regra de negócio.";

                if (result is ObjectResult objResult && objResult.Value != null)
                {
                    var valueType = objResult.Value.GetType();
                    var detailProp = valueType.GetProperty("Detail") ?? valueType.GetProperty("detail");
                    var titleProp = valueType.GetProperty("Title") ?? valueType.GetProperty("title");

                    var detailValue = detailProp?.GetValue(objResult.Value)?.ToString();
                    var titleValue = titleProp?.GetValue(objResult.Value)?.ToString();

                    log.ErrorMessage = !string.IsNullOrEmpty(detailValue)
                        ? $"{titleValue}: {detailValue}"
                        : (titleValue ?? objResult.Value.ToString());
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            log.IsSuccess = false;
            log.Message = "Erro interno no servidor.";
            log.ErrorMessage = ex.Message;
            log.StackTrace = ex.StackTrace;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            log.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            try
            {
                await logRepository.AddAsync(log);
                await unitOfWork.CommitAsync();
            }
            catch
            {
                // Evita que falhas na auditoria quebrem o fluxo principal da resposta HTTP
            }
        }
    }
}

// Requests separados dos commands quando ha parametro de rota.
public sealed record AddOrderItemRequest(long ProductId, decimal Quantity, string? Notes, long? EmployeeId);
public sealed record RaiseCreditLimitRequest(decimal NewLimitAmount);
public sealed record UpdateOrderItemStatusRequest(long OrderItemStatusId, long? ActorEmployeeId = null);
public sealed record ApplyOrderDiscountRequest(decimal DiscountAmount);
public sealed record CloseOrderRequest(decimal ServiceFeeRate = 0.10m);