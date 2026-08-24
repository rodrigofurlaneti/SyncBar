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

[Authorize(Policy = "Feature:Caixa")]
public sealed class SalesController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
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

    // Estorno: prerrogativa do gerente, apenas com a sessao de caixa aberta.
    [Authorize(Roles = "Administrador,Gerente")]
    [HttpPut("{id:long}/refund")]
    public Task<IActionResult> Refund(long id, [FromBody] RefundSaleRequest request, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(SalesController), nameof(Refund), async () =>
        {
            var result = await Mediator.Send(new RefundSaleCommand(id, request.EmployeeId, request.Reason), ct);
            return result.IsFailure ? HandleFailure(result) : NoContent();
        });

    // Pagamento parcial: cliente que sai antes deixa parte paga — SO em mesa.
    [HttpPost("partial")]
    public Task<IActionResult> RegisterPartial([FromBody] RegisterPartialPaymentCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(SalesController), nameof(RegisterPartial), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });
}

// Request separado do command quando ha parametro de rota.
public sealed record RefundSaleRequest([property: JsonRequired] long EmployeeId, string? Reason);