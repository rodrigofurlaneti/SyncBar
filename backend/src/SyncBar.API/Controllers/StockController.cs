using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Stock.AdjustInventory;
using SyncBar.Application.Features.Stock.GetByBranch;
using SyncBar.Application.Features.Stock.GetLedger;
using SyncBar.Application.Features.Stock.RegisterMovement;
using SyncBar.Application.Features.Stock.SetLimits;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Estoque")]
public sealed class StockController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("branch/{branchId:long}")]
    public async Task<IActionResult> GetByBranch(long branchId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetStockByBranchQuery(branchId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpGet("{stockItemId:long}/movements")]
    public async Task<IActionResult> GetLedger(long stockItemId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetStockLedgerQuery(stockItemId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost("movements")]
    public async Task<IActionResult> RegisterMovement([FromBody] RegisterStockMovementCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost("inventory")]
    public async Task<IActionResult> AdjustInventory([FromBody] AdjustInventoryCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPut("{id:long}/limits")]
    public async Task<IActionResult> SetLimits(long id, [FromBody] SetStockLimitsRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SetStockLimitsCommand(id, request.MinimumQuantity, request.MaximumQuantity), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
}

public sealed record SetStockLimitsRequest(decimal MinimumQuantity, decimal? MaximumQuantity);
