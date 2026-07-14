using Microsoft.AspNetCore.Mvc;

namespace SyncBar.API.Middleware;

/// <summary>
/// Captura exceções não tratadas e devolve um ProblemDetails genérico —
/// evita vazar stack trace/detalhes internos para o cliente em produção.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception processing {Method} {Path}",
                context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
                throw;

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Title = "Erro interno",
                Status = StatusCodes.Status500InternalServerError,
                Detail = environment.IsDevelopment()
                    ? ex.ToString()
                    : "Ocorreu um erro inesperado. Tente novamente ou contate o suporte."
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
