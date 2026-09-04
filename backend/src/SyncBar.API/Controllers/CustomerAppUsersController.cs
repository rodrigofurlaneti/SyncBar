using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.CustomerAppUser.Create;
using SyncBar.Application.Features.CustomerAppUser.Update;
using SyncBar.Application.Features.CustomerAppUser.Remove;
using SyncBar.Application.Features.CustomerAppUser.GetById;
using SyncBar.Application.Features.CustomerAppUser.GetByCustomerId;
using SyncBar.Application.Features.CustomerAppUser.GetByCompanyId;
using SyncBar.Application.Features.CustomerAppUser.GetByBranchId;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Authorize]
[Route("api/customerappusers")]
public sealed class CustomerAppUsersController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("{id:long}")]
    public Task<IActionResult> GetById(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CustomerAppUsersController), nameof(GetById), async () =>
        {
            var result = await Mediator.Send(new GetCustomerAppUserByIdQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("customer/{customerId:long}")]
    public Task<IActionResult> GetByCustomerId(long customerId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CustomerAppUsersController), nameof(GetByCustomerId), async () =>
        {
            var result = await Mediator.Send(new GetCustomerAppUsersByCustomerIdQuery(customerId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("company/{companyId:long}")]
    public Task<IActionResult> GetByCompanyId(long companyId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CustomerAppUsersController), nameof(GetByCompanyId), async () =>
        {
            var result = await Mediator.Send(new GetCustomerAppUsersByCompanyIdQuery(companyId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("branch/{branchId:long}")]
    public Task<IActionResult> GetByBranchId(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CustomerAppUsersController), nameof(GetByBranchId), async () =>
        {
            var result = await Mediator.Send(new GetCustomerAppUsersByBranchIdQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateCustomerAppUserCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CustomerAppUsersController), nameof(Create), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(new { id = result.Value });
        });

    [HttpPut("{id:long}")]
    public Task<IActionResult> Update(long id, [FromBody] UpdateCustomerAppUserCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CustomerAppUsersController), nameof(Update), async () =>
        {
            if (id != command.Id)
                return BadRequest("The URL ID does not match the command ID.");

            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpDelete("{id:long}")]
    public Task<IActionResult> Remove(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CustomerAppUsersController), nameof(Remove), async () =>
        {
            var result = await Mediator.Send(new RemoveCustomerAppUserCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}