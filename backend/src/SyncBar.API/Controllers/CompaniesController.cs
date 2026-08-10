using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SyncBar.Application.Features.Companies.Register;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

// Onboarding self-service: qualquer visitante pode cadastrar uma nova empresa (novo cliente do SaaS).
// Reaproveita a policy de rate limit "auth" — é tão sensível a abuso quanto login/refresh.
[EnableRateLimiting("auth")]
public sealed class CompaniesController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [AllowAnonymous]
    [HttpPost("register")]
    public Task<IActionResult> Register([FromBody] RegisterCompanyCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CompaniesController), nameof(Register), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });
}