using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Branches.Create;
using SyncBar.Application.Features.Branches.GetByCompany;
using SyncBar.Application.Features.Branches.SetSelfServiceEmployee;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

public sealed class BranchesController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("company/{companyId:long}")]
    public Task<IActionResult> GetByCompany(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(BranchesController), nameof(GetByCompany), async () =>
        {
            var result = await Mediator.Send(new GetBranchesByCompanyQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateBranchCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(BranchesController), nameof(Create), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPut("self-service-employee")]
    public Task<IActionResult> SetSelfServiceEmployee([FromBody] SetSelfServiceEmployeeCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(BranchesController), nameof(SetSelfServiceEmployee), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}