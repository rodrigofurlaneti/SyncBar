using System.Diagnostics;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Orders.UpdateItemStatus;
using SyncBar.Application.Features.Preparation.GetQueue;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

public sealed class PreparationController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("queue/branch/{branchId:long}")]
    public Task<IActionResult> GetQueue(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PreparationController), nameof(GetQueue), async () =>
        {
            var result = await Mediator.Send(new GetPreparationQueueQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("orders/{orderId:long}/items/{itemId:long}/status")]
    public Task<IActionResult> UpdateItemStatus(long orderId, long itemId,
        [FromBody] UpdateOrderItemStatusRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PreparationController), nameof(UpdateItemStatus), async () =>
        {
            var isManager = User.IsInRole("Administrador") || User.IsInRole("Gerente");
            var result = await Mediator.Send(new UpdateOrderItemStatusCommand(
                orderId, itemId, request.OrderItemStatusId, request.ActorEmployeeId, isManager), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}