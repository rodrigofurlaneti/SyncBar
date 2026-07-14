using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Reservations.Cancel;
using SyncBar.Application.Features.Reservations.Confirm;
using SyncBar.Application.Features.Reservations.Create;
using SyncBar.Application.Features.Reservations.GetByBranchAndDate;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Salao")]
public sealed class ReservationsController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("branch/{branchId:long}")]
    public async Task<IActionResult> GetByBranchAndDate(
        long branchId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetReservationsByBranchAndDateQuery(branchId, from, to), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPut("{id:long}/confirm")]
    public async Task<IActionResult> Confirm(long id, [FromBody] ConfirmReservationRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new ConfirmReservationCommand(id, request.DiningTableId), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    [HttpPut("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        var result = await Mediator.Send(new CancelReservationCommand(id), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
}

public sealed record ConfirmReservationRequest(long DiningTableId);
