using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Orders.UpdateItemStatus;
using SyncBar.Application.Features.Preparation.GetQueue;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Preparo")]
public sealed class PreparationController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("queue/branch/{branchId:long}")]
    public async Task<IActionResult> GetQueue(long branchId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPreparationQueueQuery(branchId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    // Avanco de status a partir do painel — cozinha/bar nao precisam da tela Salao.
    [HttpPut("orders/{orderId:long}/items/{itemId:long}/status")]
    public async Task<IActionResult> UpdateItemStatus(long orderId, long itemId,
        [FromBody] UpdateOrderItemStatusRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(
            new UpdateOrderItemStatusCommand(orderId, itemId, request.OrderItemStatusId), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
}
