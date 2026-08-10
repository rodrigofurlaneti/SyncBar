using System.Diagnostics;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Orders.UpdateItemStatus;
using SyncBar.Application.Features.Preparation.GetQueue;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Preparo")]
public sealed class PreparationController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("queue/branch/{branchId:long}")]
    public Task<IActionResult> GetQueue(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(GetQueue), async () =>
        {
            var result = await Mediator.Send(new GetPreparationQueueQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    // Avanco de status a partir do painel — cozinha/bar nao precisam da tela Salao.
    [HttpPut("orders/{orderId:long}/items/{itemId:long}/status")]
    public Task<IActionResult> UpdateItemStatus(long orderId, long itemId,
        [FromBody] UpdateOrderItemStatusRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(nameof(UpdateItemStatus), async () =>
        {
            var isManager = User.IsInRole("Administrador") || User.IsInRole("Gerente");
            var result = await Mediator.Send(new UpdateOrderItemStatusCommand(
                orderId, itemId, request.OrderItemStatusId, request.ActorEmployeeId, isManager), ct);
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
            ClassName = nameof(PreparationController),
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