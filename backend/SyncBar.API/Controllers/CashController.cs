using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Cash.CloseSession;
using SyncBar.Application.Features.Cash.GetOpenSession;
using SyncBar.Application.Features.Cash.GetSummary;
using SyncBar.Application.Features.Cash.OpenSession;
using SyncBar.Application.Features.Cash.RegisterMovement;

namespace SyncBar.API.Controllers;

[Authorize]
public sealed class CashController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("registers/{registerId:long}/open-session")]
    public async Task<IActionResult> GetOpenSession(long registerId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetOpenSessionQuery(registerId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpGet("sessions/{id:long}/summary")]
    public async Task<IActionResult> GetSummary(long id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCashSummaryQuery(id), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> OpenSession([FromBody] OpenCashSessionCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure
            ? HandleFailure(result)
            : CreatedAtAction(nameof(GetSummary), new { id = result.Value }, result.Value);
    }

    [HttpPut("sessions/{id:long}/close")]
    public async Task<IActionResult> CloseSession(long id, [FromBody] CloseCashSessionRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(
            new CloseCashSessionCommand(id, request.ClosedByEmployeeId, request.ClosingAmount), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost("sessions/{id:long}/movements")]
    public async Task<IActionResult> RegisterMovement(long id, [FromBody] RegisterCashMovementRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(
            new RegisterCashMovementCommand(id, request.CashMovementTypeId, request.EmployeeId, request.Amount, request.Description), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
}

// Requests separados dos commands quando ha parametro de rota.
public sealed record CloseCashSessionRequest(long ClosedByEmployeeId, decimal ClosingAmount);
public sealed record RegisterCashMovementRequest(long CashMovementTypeId, long EmployeeId, decimal Amount, string? Description);
