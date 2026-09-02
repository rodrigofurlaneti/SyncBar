using System.Diagnostics;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Application.Features.Printing.CreatePrinter;
using SyncBar.Application.Features.Printing.DeactivatePrinter;
using SyncBar.Application.Features.Printing.GetPrinters;
using SyncBar.Application.Features.Printing.SetSettings;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

public sealed class PrintersController(
    IMediator mediator,
    IPrintingService printingService,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("branch/{branchId:long}")]
    public Task<IActionResult> GetByBranch(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetByBranch), async () =>
        {
            var result = await Mediator.Send(new GetPrintersQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreatePrinterCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(Create), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("{id:long}/deactivate")]
    public Task<IActionResult> Deactivate(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(Deactivate), async () =>
        {
            var result = await Mediator.Send(new DeactivatePrinterCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("settings")]
    public Task<IActionResult> SetSettings([FromBody] SetPrintSettingsCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(SetSettings), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPost("{id:long}/test")]
    public Task<IActionResult> Test(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(Test), async () =>
        {
            var result = await printingService.PrintTestAsync(id, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    private async Task<IActionResult> ExecuteWithLogAsync(string methodName, Func<Task<IActionResult>> action)
    {
        var stopwatch = Stopwatch.StartNew();

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        long? appUserId = long.TryParse(userIdClaim, out var id) ? id : null;

        var log = new LogTracker(0)
        {
            AppUserId = appUserId,
            DirectoryName = "Controllers",
            ClassName = nameof(PrintersController),
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

                    log.ErrorMessage = !string.IsNullOrEmpty(detailValue!)
                        ? $"{titleValue}: {detailValue}"
                        : (titleValue ?? objResult.Value.ToString()!);
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