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
        var log = CreateLogEntry(className, methodName);

        try
        {
            var result = await action();
            UpdateLogFromResult(log, result);
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
            await PersistLogSafeAsync(logRepository, unitOfWork, log);
        }
    }

    private LogTracker CreateLogEntry(string className, string methodName)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        long? appUserId = long.TryParse(userIdClaim, out var id) ? id : null;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        return new LogTracker(0)
        {
            AppUserId = appUserId,
            DirectoryName = "Controllers",
            ClassName = className,
            MethodName = methodName,
            IpAddress = ipAddress,
            CreatedAt = DateTime.Now,
            IsActive = true
        };
    }

    private static void UpdateLogFromResult(LogTracker log, IActionResult result)
    {
        if (result is OkObjectResult or NoContentResult or CreatedAtActionResult)
        {
            log.IsSuccess = true;
            log.Message = "Executado com sucesso.";
            return;
        }

        log.IsSuccess = false;
        log.Message = "Falha na regra de negócio.";

        if (result is ObjectResult { Value: not null } objResult)
        {
            log.ErrorMessage = ExtractErrorMessage(objResult.Value);
        }
    }

    private static string? ExtractErrorMessage(object value)
    {
        if (value is ProblemDetails problemDetails)
        {
            return !string.IsNullOrEmpty(problemDetails.Detail)
                ? $"{problemDetails.Title}: {problemDetails.Detail}"
                : problemDetails.Title;
        }

        var valueType = value.GetType();
        var detailProp = valueType.GetProperty("Detail") ?? valueType.GetProperty("detail");
        var titleProp = valueType.GetProperty("Title") ?? valueType.GetProperty("title");

        var detailValue = detailProp?.GetValue(value)?.ToString();
        var titleValue = titleProp?.GetValue(value)?.ToString();

        return !string.IsNullOrEmpty(detailValue)
            ? $"{titleValue}: {detailValue}"
            : (titleValue ?? value.ToString()!);
    }

    private static async Task PersistLogSafeAsync(
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork,
        LogTracker log)
    {
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

    // --- SOBRECARGA 2: Resolve os repositórios via DI automaticamente (3 argumentos) ---
    //
    // Achado de revisão: isto criava um escopo de DI FILHO novo (`HttpContext.RequestServices
    // .CreateScope()`), o que resolve um `AppDbContext`/`IUnitOfWork` DIFERENTE do que o resto da
    // requisição usa (o mediator/handlers chamados por `action()` usam o `AppDbContext` do escopo
    // raiz da requisição). Isso não é a mesma corrida do bug do `BaseCommandHandler.cs` (cada
    // `AppDbContext` aqui é uma instância própria, não compartilhada entre threads), mas abre uma
    // segunda conexão MySQL por requisição só para gravar o log — desnecessário, já que
    // `HttpContext.RequestServices` já É o próprio escopo da requisição e resolve os mesmos
    // `ILogTrackerRepository`/`IUnitOfWork` (mesmo `AppDbContext`) usados pelo resto do fluxo, sem
    // precisar criar um escopo filho. Trocado para resolver direto do escopo da requisição.
    protected async Task<IActionResult> ExecuteWithLogAsync(
        string className,
        string methodName,
        Func<Task<IActionResult>> action)
    {
        var logRepo = HttpContext.RequestServices.GetRequiredService<ILogTrackerRepository>();
        var uow = HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();

        return await ExecuteWithLogAsync(logRepo, uow, className, methodName, action);
    }
}