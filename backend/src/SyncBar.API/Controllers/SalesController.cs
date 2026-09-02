using System.Security.Claims;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Billing.GetSalesBySession;
using SyncBar.Application.Features.Billing.RefundSale;
using SyncBar.Application.Features.Billing.RegisterPartialPayment;
using SyncBar.Application.Features.Billing.RegisterSale;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

public sealed class SalesController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [Authorize(Roles = ManagerRoles)]
    [HttpPost]
    public Task<IActionResult> Register([FromBody] RegisterSaleCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(SalesController), nameof(Register), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [HttpGet("session/{sessionId:long}")]
    public Task<IActionResult> GetBySession(long sessionId, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(SalesController), nameof(GetBySession), async () =>
        {
            var result = await Mediator.Send(new GetSalesBySessionQuery(sessionId), ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPut("{id:long}/refund")]
    public Task<IActionResult> Refund(long id, [FromBody] RefundSaleRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(SalesController), nameof(Refund), async () =>
        {
            var result = await Mediator.Send(new RefundSaleCommand(id, request.EmployeeId, request.Reason), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    [Authorize(Roles = ManagerRoles)]
    [HttpPost("partial")]
    public Task<IActionResult> RegisterPartial([FromBody] RegisterPartialPaymentCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(SalesController), nameof(RegisterPartial), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });
}

public sealed record RefundSaleRequest([property: JsonRequired] long EmployeeId, string? Reason);