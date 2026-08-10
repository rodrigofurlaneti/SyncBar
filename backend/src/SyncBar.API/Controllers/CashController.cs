using System.Diagnostics;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Cash.CloseSession;
using SyncBar.Application.Features.Cash.GetHistory;
using SyncBar.Application.Features.Cash.GetOpenSession;
using SyncBar.Application.Features.Cash.ReviewSession;
using SyncBar.Application.Features.Cash.GetSummary;
using SyncBar.Application.Features.Cash.OpenSession;
using SyncBar.Application.Features.Cash.RegisterMovement;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Caixa")]
public sealed class CashController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("registers/{registerId:long}/open-session")]
    public Task<IActionResult> GetOpenSession(long registerId, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetOpenSession), async () =>
        {
            var result = await Mediator.Send(new GetOpenSessionQuery(registerId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("sessions/{id:long}/summary")]
    public Task<IActionResult> GetSummary(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetSummary), async () =>
        {
            var result = await Mediator.Send(new GetCashSummaryQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("history/branch/{branchId:long}/{year:int}/{month:int}")]
    public Task<IActionResult> GetHistory(long branchId, int year, int month, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetHistory), async () =>
        {
            var result = await Mediator.Send(new GetCashSessionHistoryQuery(branchId, year, month), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("sessions/{id:long}/review")]
    public Task<IActionResult> ReviewSession(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(ReviewSession), async () =>
        {
            var result = await Mediator.Send(new ReviewCashSessionCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("sessions")]
    public Task<IActionResult> OpenSession([FromBody] OpenCashSessionCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(OpenSession), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure
                ? HandleFailure(result)
                : CreatedAtAction(nameof(GetSummary), new { id = result.Value }, result.Value);
        });

    [HttpPut("sessions/{id:long}/close")]
    public Task<IActionResult> CloseSession(long id, [FromBody] CloseCashSessionRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(CloseSession), async () =>
        {
            var result = await Mediator.Send(
                new CloseCashSessionCommand(id, request.ClosedByEmployeeId, request.ClosingAmount), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("sessions/{id:long}/movements")]
    public Task<IActionResult> RegisterMovement(long id, [FromBody] RegisterCashMovementRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(RegisterMovement), async () =>
        {
            var result = await Mediator.Send(
                new RegisterCashMovementCommand(id, request.CashMovementTypeId, request.EmployeeId, request.Amount, request.Description), ct);
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
            ClassName = nameof(CashController),
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
public sealed record CloseCashSessionRequest(long ClosedByEmployeeId, decimal ClosingAmount);
public sealed record RegisterCashMovementRequest(long CashMovementTypeId, long EmployeeId, decimal Amount, string? Description);