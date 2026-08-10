using System.Diagnostics;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Reservations.Cancel;
using SyncBar.Application.Features.Reservations.Confirm;
using SyncBar.Application.Features.Reservations.Create;
using SyncBar.Application.Features.Reservations.GetByBranchAndDate;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Salao")]
public sealed class ReservationsController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("branch/{branchId:long}")]
    public Task<IActionResult> GetByBranchAndDate(
        long branchId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ReservationsController), nameof(GetByBranchAndDate), async () =>
        {
            var result = await Mediator.Send(new GetReservationsByBranchAndDateQuery(branchId, from, to), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateReservationCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ReservationsController), nameof(Create), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("{id:long}/confirm")]
    public Task<IActionResult> Confirm(long id, [FromBody] ConfirmReservationRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ReservationsController), nameof(Confirm), async () =>
        {
            var result = await Mediator.Send(new ConfirmReservationCommand(id, request.DiningTableId), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("{id:long}/cancel")]
    public Task<IActionResult> Cancel(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(ReservationsController), nameof(Cancel), async () =>
        {
            var result = await Mediator.Send(new CancelReservationCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record ConfirmReservationRequest(long DiningTableId);