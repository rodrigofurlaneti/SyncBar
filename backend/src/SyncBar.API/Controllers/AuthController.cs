using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SyncBar.Application.Features.Auth.Login;
using SyncBar.Application.Features.Auth.Refresh;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[EnableRateLimiting("auth")]
public sealed class AuthController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [AllowAnonymous]
    [HttpPost("login")]
    public Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AuthController), nameof(Login), async () =>
        {
            var enriched = command with
            {
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua[..Math.Min(ua.Length, 300)] : null,
            };
            var result = await Mediator.Send(enriched, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [AllowAnonymous]
    [HttpPost("refresh")]
    public Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(AuthController), nameof(Refresh), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });
}