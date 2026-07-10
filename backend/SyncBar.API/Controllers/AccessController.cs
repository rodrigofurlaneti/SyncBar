using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Access.GetFeatures;
using SyncBar.Application.Features.Access.GetJobTitleFeatures;
using SyncBar.Application.Features.Access.GetMyFeatures;
using SyncBar.Application.Features.Access.GetUserFeatures;
using SyncBar.Application.Features.Access.SetJobTitleFeatures;
using SyncBar.Application.Features.Access.SetUserFeatures;
using SyncBar.Domain.Constants;

namespace SyncBar.API.Controllers;

[Authorize]
public sealed class AccessController(IMediator mediator) : ApiController(mediator)
{
    private const string ManagerRoles = "Administrador,Gerente";

    // Qualquer usuario autenticado consulta as proprias telas.
    [HttpGet("my-features")]
    public async Task<IActionResult> GetMyFeatures(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!long.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var isManager = FeatureCodes.ManagerRoles.Any(User.IsInRole);
        var result = await Mediator.Send(new GetMyFeaturesQuery(userId, isManager), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    // Gestao de acessos: somente Gerente/Administrador.
    [Authorize(Roles = ManagerRoles)]
    [HttpGet("features")]
    public async Task<IActionResult> GetFeatures(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetFeaturesQuery(), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [Authorize(Roles = ManagerRoles)]
    [HttpGet("jobtitles/{jobTitleId:long}/features")]
    public async Task<IActionResult> GetJobTitleFeatures(long jobTitleId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetJobTitleFeaturesQuery(jobTitleId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [Authorize(Roles = ManagerRoles)]
    [HttpPut("jobtitles/{jobTitleId:long}/features")]
    public async Task<IActionResult> SetJobTitleFeatures(long jobTitleId, [FromBody] SetFeaturesRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SetJobTitleFeaturesCommand(jobTitleId, request.FeatureIds), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    [Authorize(Roles = ManagerRoles)]
    [HttpGet("users/{appUserId:long}/features")]
    public async Task<IActionResult> GetUserFeatures(long appUserId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetUserFeaturesQuery(appUserId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [Authorize(Roles = ManagerRoles)]
    [HttpPut("users/{appUserId:long}/features")]
    public async Task<IActionResult> SetUserFeatures(long appUserId, [FromBody] SetFeaturesRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SetUserFeaturesCommand(appUserId, request.FeatureIds), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
}

public sealed record SetFeaturesRequest(IReadOnlyCollection<long> FeatureIds);
