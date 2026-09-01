using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Users.Create;
using SyncBar.Application.Features.Users.CreateRole;
using SyncBar.Application.Features.Users.Deactivate;
using SyncBar.Application.Features.Users.GetByCompany;
using SyncBar.Application.Features.Users.GetRoles;
using SyncBar.Application.Features.Users.UpdateRoles;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

public sealed class UsersController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("company/{companyId:long}")]
    public Task<IActionResult> GetByCompany(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(UsersController), nameof(GetByCompany), async () =>
        {
            var result = await Mediator.Send(new GetUsersByCompanyQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("roles/company/{companyId:long}")]
    public Task<IActionResult> GetRoles(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(UsersController), nameof(GetRoles), async () =>
        {
            var result = await Mediator.Send(new GetRolesQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateUserCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(UsersController), nameof(Create), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure
                ? HandleFailure(result)
                : CreatedAtAction(nameof(GetByCompany), new { companyId = command.CompanyId }, result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPost("roles")]
    public Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(UsersController), nameof(CreateRole), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure
                ? HandleFailure(result)
                : CreatedAtAction(nameof(GetRoles), new { companyId = command.CompanyId }, result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPut("{id:long}/roles")]
    public Task<IActionResult> UpdateRoles(long id, [FromBody] UpdateUserRolesRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(UsersController), nameof(UpdateRoles), async () =>
        {
            var result = await Mediator.Send(new UpdateUserRolesCommand(id, request.RoleIds), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPut("{id:long}/deactivate")]
    public Task<IActionResult> Deactivate(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(UsersController), nameof(Deactivate), async () =>
        {
            var result = await Mediator.Send(new DeactivateUserCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record UpdateUserRolesRequest(IReadOnlyCollection<long> RoleIds);