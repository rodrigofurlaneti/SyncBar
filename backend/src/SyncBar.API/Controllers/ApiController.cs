using System.Diagnostics;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiController(IMediator mediator) : ControllerBase
{
    protected readonly IMediator Mediator = mediator;

    protected IActionResult HandleFailure(Result result)
        => result.Error.Code switch
        {
            var c when c.EndsWith(".NotFound") => NotFound(CreateProblemDetails(result)),
            var c when c.EndsWith(".AlreadyExists") => Conflict(CreateProblemDetails(result)),
            var c when c.EndsWith(".Duplicate") => Conflict(CreateProblemDetails(result)),
            _ => BadRequest(CreateProblemDetails(result))
        };

    protected static ProblemDetails CreateProblemDetails(Result result)
        => new() { Title = result.Error.Code, Detail = result.Error.Message };

    // --- SOBRECARGA 1: Recebe os repositórios explicitamente (5 argumentos) ---
    protected async Task<IActionResult> ExecuteWithLogAsync(
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork,
        string className,
        string methodName,
        Func<Task<IActionResult>> action)
    {
        var stopwatch = Stopwatch.StartNew();

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        long? appUserId = long.TryParse(userIdClaim, out var id) ? id : null;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var log = new LogTracker(0)
        {
            AppUserId = appUserId,
            DirectoryName = "Controllers",
            ClassName = className,
            MethodName = methodName,
            IpAddress = ipAddress,
            CreatedAt = DateTime.Now,
            IsActive = true
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
                    if (objResult.Value is ProblemDetails problemDetails)
                    {
                        log.ErrorMessage = !string.IsNullOrEmpty(problemDetails.Detail)
                            ? $"{problemDetails.Title}: {problemDetails.Detail}"
                            : problemDetails.Title;
                    }
                    else
                    {
                        var valueType = objResult.Value.GetType();
                        var detailProp = valueType.GetProperty("Detail") ?? valueType.GetProperty("detail");
                        var titleProp = valueType.GetProperty("Title") ?? valueType.GetProperty("title");

                        var detailValue = detailProp?.GetValue(objResult.Value)?.ToString();
                        var titleValue = titleProp?.GetValue(objResult.Value)?.ToString();

                        log.ErrorMessage = !string.IsNullOrEmpty(detailValue)
                            ? $"{titleValue}: {detailValue}"
                            : (titleValue ?? objResult.Value.ToString()!);
                    }
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

    // --- SOBRECARGA 2: Resolve os repositórios via DI automaticamente (3 argumentos) ---
    protected async Task<IActionResult> ExecuteWithLogAsync(
        string className,
        string methodName,
        Func<Task<IActionResult>> action)
    {
        using var scope = HttpContext.RequestServices.CreateScope();
        var logRepo = scope.ServiceProvider.GetRequiredService<ILogTrackerRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        return await ExecuteWithLogAsync(logRepo, uow, className, methodName, action);
    }
}