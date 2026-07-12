using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Finance.CreateCost;
using SyncBar.Application.Features.Finance.DeactivateCost;
using SyncBar.Application.Features.Finance.GetSalesReport;
using SyncBar.Application.Features.Finance.GetScenarios;
using SyncBar.Application.Features.Finance.GetSummary;
using SyncBar.Application.Features.Finance.SetTarget;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Faturamento")]
public sealed class FinanceController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("summary/branch/{branchId:long}/{year:int}/{month:int}")]
    public async Task<IActionResult> GetSummary(long branchId, int year, int month, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetBillingSummaryQuery(branchId, year, month), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpGet("reports/sales/branch/{branchId:long}/{year:int}/{month:int}")]
    public async Task<IActionResult> GetSalesReport(long branchId, int year, int month, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetSalesReportQuery(branchId, year, month), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpGet("scenarios/branch/{branchId:long}/{year:int}/{month:int}")]
    public async Task<IActionResult> GetScenarios(
        long branchId, int year, int month,
        [FromQuery] decimal desiredProfit = 0,
        [FromQuery] decimal? pessimisticMargin = null,
        [FromQuery] decimal? normalMargin = null,
        [FromQuery] decimal? optimisticMargin = null,
        CancellationToken ct = default)
    {
        var result = await Mediator.Send(new GetScenariosQuery(
            branchId, year, month, desiredProfit, pessimisticMargin, normalMargin, optimisticMargin), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost("costs")]
    public async Task<IActionResult> CreateCost([FromBody] CreateOperatingCostCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPut("costs/{id:long}/deactivate")]
    public async Task<IActionResult> DeactivateCost(long id, CancellationToken ct)
    {
        var result = await Mediator.Send(new DeactivateOperatingCostCommand(id), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }

    [HttpPut("target")]
    public async Task<IActionResult> SetTarget([FromBody] SetRevenueTargetCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }
}
