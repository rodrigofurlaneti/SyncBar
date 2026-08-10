using System.Diagnostics;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Application.Features.Printing.GetSettings;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

// Acoes de impressao usadas na operacao (salao/caixa) — qualquer usuario autenticado.
[Authorize]
public sealed class PrintingController(
    IMediator mediator,
    IPrintingService printingService,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    // O frontend consulta para decidir se mostra o "Deseja imprimir?".
    [HttpGet("settings/branch/{branchId:long}")]
    public Task<IActionResult> GetSettings(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetSettings), async () =>
        {
            var result = await Mediator.Send(new GetPrintSettingsQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("bill/{orderId:long}")]
    public Task<IActionResult> PrintBill(long orderId, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(PrintBill), async () =>
        {
            var result = await printingService.PrintBillAsync(orderId, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("receipt/{saleId:long}")]
    public Task<IActionResult> PrintReceipt(long saleId, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(PrintReceipt), async () =>
        {
            var result = await printingService.PrintPaymentReceiptAsync(saleId, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("partial-receipt/{partialId:long}")]
    public Task<IActionResult> PrintPartialReceipt(long partialId, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(PrintPartialReceipt), async () =>
        {
            var result = await printingService.PrintPartialReceiptAsync(partialId, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("cash-session/{sessionId:long}")]
    public Task<IActionResult> PrintCashClosing(long sessionId, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(PrintCashClosing), async () =>
        {
            var result = await printingService.PrintCashClosingAsync(sessionId, ct);
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
            ClassName = nameof(PrintingController),
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