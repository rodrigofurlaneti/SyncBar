using System.Security.Claims;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Cash.CloseSession;
using SyncBar.Application.Features.Cash.GetHistory;
using SyncBar.Application.Features.Cash.GetOpenSession;
using SyncBar.Application.Features.Cash.ReviewSession;
using SyncBar.Application.Features.Cash.GetSummary;
using SyncBar.Application.Features.Cash.OpenSession;
using SyncBar.Application.Features.Cash.RegisterMovement;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

public sealed class CashController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpGet("registers/{registerId:long}/open-session")]
    public Task<IActionResult> GetOpenSession(long registerId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CashController), nameof(GetOpenSession), async () =>
        {
            var result = await Mediator.Send(new GetOpenSessionQuery(registerId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("sessions/{id:long}/summary")]
    public Task<IActionResult> GetSummary(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CashController), nameof(GetSummary), async () =>
        {
            var result = await Mediator.Send(new GetCashSummaryQuery(id), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("history/branch/{branchId:long}/{year:int}/{month:int}")]
    public Task<IActionResult> GetHistory(long branchId, int year, int month, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CashController), nameof(GetHistory), async () =>
        {
            var result = await Mediator.Send(new GetCashSessionHistoryQuery(branchId, year, month), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPut("sessions/{id:long}/review")]
    public Task<IActionResult> ReviewSession(long id, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CashController), nameof(ReviewSession), async () =>
        {
            var result = await Mediator.Send(new ReviewCashSessionCommand(id), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPost("sessions")]
    public Task<IActionResult> OpenSession([FromBody] OpenCashSessionCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CashController), nameof(OpenSession), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure
                ? HandleFailure(result)
                : CreatedAtAction(nameof(GetSummary), new { id = result.Value }, result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPut("sessions/{id:long}/close")]
    public Task<IActionResult> CloseSession(long id, [FromBody] CloseCashSessionRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CashController), nameof(CloseSession), async () =>
        {
            var result = await Mediator.Send(
                new CloseCashSessionCommand(id, request.ClosedByEmployeeId, request.ClosingAmount), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPost("sessions/{id:long}/movements")]
    public Task<IActionResult> RegisterMovement(long id, [FromBody] RegisterCashMovementRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(CashController), nameof(RegisterMovement), async () =>
        {
            var result = await Mediator.Send(
                new RegisterCashMovementCommand(id, request.CashMovementTypeId, request.EmployeeId, request.Amount, request.Description), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });
}

public sealed record CloseCashSessionRequest(
    [property: JsonRequired] long ClosedByEmployeeId,
    [property: JsonRequired] decimal ClosingAmount);
public sealed record RegisterCashMovementRequest(
    [property: JsonRequired] long CashMovementTypeId,
    [property: JsonRequired] long EmployeeId,
    [property: JsonRequired] decimal Amount,
    string? Description);