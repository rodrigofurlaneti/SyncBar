using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Payments.ChargePayment;
using SyncBar.Domain.Repositories;

namespace SyncBar.API.Controllers;

// Cobrança via gateway (Pix/cartão). Implementação padrão é fake — troque o registro de
// IPaymentGatewayService em SyncBar.Infrastructure.DependencyInjection por um provider real
// (ex.: Focus NFe, eNotas / MercadoPago) antes de usar em produção.
[Authorize(Policy = "Feature:Caixa")]
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