using System.Diagnostics;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Stock.AdjustInventory;
using SyncBar.Application.Features.Stock.GetByBranch;
using SyncBar.Application.Features.Stock.GetLedger;
using SyncBar.Application.Features.Stock.RegisterMovement;
using SyncBar.Application.Features.Stock.SetLimits;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Estoque")]
public sealed class StockController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("branch/{branchId:long}")]
    public Task<IActionResult> GetByBranch(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetByBranch), async () =>
        {
            var result = await Mediator.Send(new GetStockByBranchQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("{stockItemId:long}/movements")]
    public Task<IActionResult> GetLedger(long stockItemId, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetLedger), async () =>
        {
            var result = await Mediator.Send(new GetStockLedgerQuery(stockItemId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("movements")]
    public Task<IActionResult> RegisterMovement([FromBody] RegisterStockMovementCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(RegisterMovement), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("inventory")]
    public Task<IActionResult> AdjustInventory([FromBody] AdjustInventoryCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(AdjustInventory), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("{id:long}/limits")]
    public Task<IActionResult> SetLimits(long id, [FromBody] SetStockLimitsRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(SetLimits), async () =>
        {
            var result = await Mediator.Send(new SetStockLimitsCommand(id, request.MinimumQuantity, request.MaximumQuantity), ct);
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
            ClassName = nameof(StockController),
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

public sealed record SetStockLimitsRequest(decimal MinimumQuantity, decimal? MaximumQuantity);