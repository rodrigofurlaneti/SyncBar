using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SyncBar.Application.Features.Companies.Register;

namespace SyncBar.API.Controllers;

// Onboarding self-service: qualquer visitante pode cadastrar uma nova empresa (novo cliente do SaaS).
// Reaproveita a policy de rate limit "auth" — é tão sensível a abuso quanto login/refresh.
[EnableRateLimiting("auth")]
public sealed class CompaniesController(IMediator mediator) : ApiController(mediator)
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCompanyCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }
}
