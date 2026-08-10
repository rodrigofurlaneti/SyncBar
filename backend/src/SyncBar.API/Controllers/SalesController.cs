using System.Diagnostics;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Billing.GetSalesBySession;
using SyncBar.Application.Features.Billing.RefundSale;
using SyncBar.Application.Features.Billing.RegisterPartialPayment;
using SyncBar.Application.Features.Billing.RegisterSale;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Caixa")]
public sealed class SalesController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpPost]
    public Task<IActionResult> Register([FromBody] RegisterSaleCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(Register), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("session/{sessionId:long}")]
    public Task<IActionResult> GetBySession(long sessionId, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetBySession), async () =>
        {
            var result = await Mediator.Send(new GetSalesBySessionQuery(sessionId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Estorno: prerrogativa do gerente, apenas com a sessao de caixa aberta.
    [Authorize(Roles = "Administrador,Gerente")]
    [HttpPut("{id:long}/refund")]
    public Task<IActionResult> Refund(long id, [FromBody] RefundSaleRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(Refund), async () =>
        {
            var result = await Mediator.Send(new RefundSaleCommand(id, request.EmployeeId, request.Reason), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Pagamento parcial: cliente que sai antes deixa parte paga — SO em mesa.
    [HttpPost("partial")]
    public Task<IActionResult> RegisterPartial([FromBody] RegisterPartialPaymentCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(RegisterPartial), async () =>
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
            ClassName = nameof(SalesController),
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

// Request separado do command quando ha parametro de rota.
public sealed record RefundSaleRequest(long EmployeeId, string? Reason);