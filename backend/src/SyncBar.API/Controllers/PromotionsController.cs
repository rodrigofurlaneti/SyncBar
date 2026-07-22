using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Promotions.Create;
using SyncBar.Application.Features.Promotions.Deactivate;
using SyncBar.Application.Features.Promotions.GetByBranch;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Promocoes")]
public sealed class PromotionsController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("branch/{branchId:long}")]
    public async Task<IActionResult> GetByBranch(long branchId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPromotionsByBranchQuery(branchId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePromotionCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPut("{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        var result = await Mediator.Send(new DeactivatePromotionCommand(id), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
}
