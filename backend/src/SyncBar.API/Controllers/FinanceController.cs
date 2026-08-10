using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Finance.CreateCost;
using SyncBar.Application.Features.Finance.DeactivateCost;
using SyncBar.Application.Features.Finance.GetCommissionReport;
using SyncBar.Application.Features.Finance.GetSalesReport;
using SyncBar.Application.Features.Finance.GetScenarios;
using SyncBar.Application.Features.Finance.GetSummary;
using SyncBar.Application.Features.Finance.SetTarget;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Faturamento")]
public sealed class FinanceController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("summary/branch/{branchId:long}/{year:int}/{month:int}")]
    public Task<IActionResult> GetSummary(long branchId, int year, int month, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(FinanceController), nameof(GetSummary), async () =>
        {
            var result = await Mediator.Send(new GetBillingSummaryQuery(branchId, year, month), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("reports/sales/branch/{branchId:long}/{year:int}/{month:int}")]
    public Task<IActionResult> GetSalesReport(long branchId, int year, int month, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(FinanceController), nameof(GetSalesReport), async () =>
        {
            var result = await Mediator.Send(new GetSalesReportQuery(branchId, year, month), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("scenarios/branch/{branchId:long}/{year:int}/{month:int}")]
    public Task<IActionResult> GetScenarios(
        long branchId, int year, int month,
        [FromQuery] decimal desiredProfit = 0,
        [FromQuery] decimal? pessimisticMargin = null,
        [FromQuery] decimal? normalMargin = null,
        [FromQuery] decimal? optimisticMargin = null,
        CancellationToken ct = default) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(FinanceController), nameof(GetScenarios), async () =>
        {
            var result = await Mediator.Send(new GetScenariosQuery(
                branchId, year, month, desiredProfit, pessimisticMargin, normalMargin, optimisticMargin), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("commissions/branch/{branchId:long}")]
    public Task<IActionResult> GetCommissionReport(
        long branchId, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(FinanceController), nameof(GetCommissionReport), async () =>
        {
            var result = await Mediator.Send(new GetCommissionReportQuery(branchId, from, to), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPost("costs")]
    public Task<IActionResult> CreateCost([FromBody] CreateOperatingCostCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(FinanceController), nameof(CreateCost), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpPut("costs/{id:long}/deactivate")]
    public Task<IActionResult> DeactivateCost(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(FinanceController), nameof(DeactivateCost), async () =>
        {
            var result = await Mediator.Send(new DeactivateOperatingCostCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [HttpPut("target")]
    public Task<IActionResult> SetTarget([FromBody] SetRevenueTargetCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(FinanceController), nameof(SetTarget), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });
}