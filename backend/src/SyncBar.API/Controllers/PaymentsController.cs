using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Payments.ChargePayment;

namespace SyncBar.API.Controllers;

// Cobrança via gateway (Pix/cartão). Implementação padrão é fake — troque o registro de
// IPaymentGatewayService em SyncBar.Infrastructure.DependencyInjection por um provider real
// (ex.: MercadoPago) antes de usar em produção.
[Authorize(Policy = "Feature:Caixa")]
public sealed class PaymentsController(IMediator mediator) : ApiController(mediator)
{
    [HttpPost("charge")]
    public async Task<IActionResult> Charge([FromBody] ChargePaymentCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }
}
