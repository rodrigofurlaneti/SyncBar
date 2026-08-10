using System.Diagnostics;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Finance.CreateCost;
using SyncBar.Application.Features.Finance.DeactivateCost;
using SyncBar.Application.Features.Finance.GetCommissionReport;
using SyncBar.Application.Features.Finance.GetSalesReport;
using SyncBar.Application.Features.Finance.GetScenarios;
using SyncBar.Application.Features.Finance.GetSummary;
using SyncBar.Application.Features.Finance.SetTarget;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Faturamento")]
public sealed class FinanceController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("summary/branch/{branchId:long}/{year:int}/{month:int}")]
    public Task<IActionResult> GetSummary(long branchId, int year, int month, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetSummary), async () =>
        {
            var result = await Mediator.Send(new GetBillingSummaryQuery(branchId, year, month), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("reports/sales/branch/{branchId:long}/{year:int}/{month:int}")]
    public Task<IActionResult> GetSalesReport(long branchId, int year, int month, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetSalesReport), async () =>
        {
            var result = await Mediator.Send(new GetSalesReportQuery(branchId, year, month), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("scenarios/branch/{branchId:long}/{year:int}/{month:int}")]
    public Task<IActionResult> GetScenarios(
        long branchId, int year, int month,
        [FromQuery] decimal desiredProfit = 0,
        [FromQuery] decimal? pessimisticMargin = null,
        [FromQuery] decimal? normalMargin = null,
        [FromQuery] decimal? optimisticMargin = null,
        CancellationToken ct = default) =>
        ExecuteWithLogAsync(nameof(GetScenarios), async () =>
        {
            var result = await Mediator.Send(new GetScenariosQuery(
                branchId, year, month, desiredProfit, pessimisticMargin, normalMargin, optimisticMargin), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("commissions/branch/{branchId:long}")]
    public Task<IActionResult> GetCommissionReport(
        long branchId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetCommissionReport), async () =>
        {
            var result = await Mediator.Send(new GetCommissionReportQuery(branchId, from, to), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("costs")]
    public Task<IActionResult> CreateCost([FromBody] CreateOperatingCostCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(CreateCost), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("costs/{id:long}/deactivate")]
    public Task<IActionResult> DeactivateCost(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(DeactivateCost), async () =>
        {
            var result = await Mediator.Send(new DeactivateOperatingCostCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("target")]
    public Task<IActionResult> SetTarget([FromBody] SetRevenueTargetCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(SetTarget), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
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
            ClassName = nameof(FinanceController),
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