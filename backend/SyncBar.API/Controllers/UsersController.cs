using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Users.Create;
using SyncBar.Application.Features.Users.Deactivate;
using SyncBar.Application.Features.Users.GetByCompany;
using SyncBar.Application.Features.Users.GetRoles;
using SyncBar.Application.Features.Users.UpdateRoles;

namespace SyncBar.API.Controllers;

[Authorize]
public sealed class UsersController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("company/{companyId:long}")]
    public async Task<IActionResult> GetByCompany(long companyId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetUsersByCompanyQuery(companyId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpGet("roles/company/{companyId:long}")]
    public async Task<IActionResult> GetRoles(long companyId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetRolesQuery(companyId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure
            ? HandleFailure(result)
            : CreatedAtAction(nameof(GetByCompany), new { companyId = command.CompanyId }, result.Value);
    }

    [HttpPut("{id:long}/roles")]
    public async Task<IActionResult> UpdateRoles(long id, [FromBody] UpdateUserRolesRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new UpdateUserRolesCommand(id, request.RoleIds), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    [HttpPut("{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        var result = await Mediator.Send(new DeactivateUserCommand(id), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
}

public sealed record UpdateUserRolesRequest(IReadOnlyCollection<long> RoleIds);
