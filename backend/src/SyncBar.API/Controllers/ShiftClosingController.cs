using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Shift.CloseShift;
using SyncBar.Application.Features.Shift.GetById;
using SyncBar.Application.Features.Shift.OpenShift;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

public sealed class ShiftClosingController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("{id:long}")]
    public Task<IActionResult> GetById(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ShiftClosingController), nameof(GetById), async () =>
        {
            var result = await Mediator.Send(new GetShiftClosingByIdQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPost]
    public Task<IActionResult> Open([FromBody] OpenShiftClosingCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ShiftClosingController), nameof(Open), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure
                ? HandleFailure(result)
                : CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPut("{id:long}/close")]
    public Task<IActionResult> Close(long id, [FromBody] CloseShiftClosingRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ShiftClosingController), nameof(Close), async () =>
        {
            var result = await Mediator.Send(
                new CloseShiftClosingCommand(id, request.ClosedByEmployeeId, request.Notes), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });
}

public sealed record CloseShiftClosingRequest(
    [property: JsonRequired] long ClosedByEmployeeId,
    string? Notes);
