using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Payments.ChargePayment;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Integrations.Asaas;
using System.Security.Claims;

namespace SyncBar.API.Controllers;

public sealed class PaymentsController(
    IMediator mediator,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : ApiController(mediator)
{
    [HttpPost("charge")]
    public Task<IActionResult> Charge([FromBody] ChargePaymentCommand command, CancellationToken ct) =>
        ExecuteWithLogAsync(logRepository, unitOfWork, nameof(PaymentsController), nameof(Charge), async () =>
        {
            var result = await Mediator.Send(command, ct);
            return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
        });
}