using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Stock.AdjustInventory;
using SyncBar.Application.Features.Stock.GetByBranch;
using SyncBar.Application.Features.Stock.GetLedger;
using SyncBar.Application.Features.Stock.RegisterMovement;
using SyncBar.Application.Features.Stock.SetLimits;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Estoque")]
public sealed class StockController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("branch/{branchId:long}")]
    public Task<IActionResult> GetByBranch(long branchId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(StockController), nameof(GetByBranch), async () =>
        {
            var result = await Mediator.Send(new GetStockByBranchQuery(branchId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("{stockItemId:long}/movements")]
    public Task<IActionResult> GetLedger(long stockItemId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(StockController), nameof(GetLedger), async () =>
        {
            var result = await Mediator.Send(new GetStockLedgerQuery(stockItemId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("movements")]
    public Task<IActionResult> RegisterMovement([FromBody] RegisterStockMovementCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(StockController), nameof(RegisterMovement), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("inventory")]
    public Task<IActionResult> AdjustInventory([FromBody] AdjustInventoryCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(StockController), nameof(AdjustInventory), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("{id:long}/limits")]
    public Task<IActionResult> SetLimits(long id, [FromBody] SetStockLimitsRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(StockController), nameof(SetLimits), async () =>
        {
            var result = await Mediator.Send(new SetStockLimitsCommand(id, request.MinimumQuantity, request.MaximumQuantity), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record SetStockLimitsRequest([property: JsonRequired] decimal MinimumQuantity, decimal? MaximumQuantity);