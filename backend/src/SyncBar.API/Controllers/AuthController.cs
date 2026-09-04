using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SyncBar.Application.Features.Auth.Login;
using SyncBar.Application.Features.Auth.Refresh;
using SyncBar.Application.Features.Auth.CustomerLogin;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using System.Diagnostics;
using System.Security.Claims;

namespace SyncBar.API.Controllers;

[EnableRateLimiting("auth")]
public sealed class AuthController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork,
    ILogger<AuthController> logger) : ApiController(mediator)
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var enriched = command with
        {
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua[..Math.Min(ua.Length, 300)] : null,
        };
        var result = await Mediator.Send(enriched, ct);
        stopwatch.Stop();
        var log = new LogTracker(0)
        {
            DirectoryName = "Controllers",
            ClassName = nameof(AuthController),
            MethodName = nameof(Login),
            IsSuccess = !result.IsFailure,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
            IpAddress = enriched.IpAddress,
            CreatedAt = DateTime.Now
        };

        try
        {
            await logRepository.AddAsync(log, CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao gravar log de auditoria de login.");
        }

        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }
    [AllowAnonymous]
    [HttpPost("customer-login")]
    public async Task<IActionResult> CustomerLogin([FromBody] CustomerLoginCommand command, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var enriched = command with
        {
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua[..Math.Min(ua.Length, 300)] : null,
        };
        var result = await Mediator.Send(enriched, ct);
        stopwatch.Stop();
        var log = new LogTracker(0)
        {
            DirectoryName = "Controllers",
            ClassName = nameof(AuthController),
            MethodName = nameof(CustomerLogin),
            IsSuccess = !result.IsFailure,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
            IpAddress = enriched.IpAddress,
            CreatedAt = DateTime.Now
        };
        try
        {
            await logRepository.AddAsync(log, CancellationToken.None);
            await unitOfWork.CommitAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao gravar log de auditoria do login de cliente.");
        }
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }
    [AllowAnonymous]
    [HttpPost("refresh")]
    public Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AuthController), nameof(Refresh), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });
}