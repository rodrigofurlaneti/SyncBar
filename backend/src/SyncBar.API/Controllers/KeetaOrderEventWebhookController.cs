using MediatR;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Integrations.Keeta.Order.Polling;

namespace SyncBar.API.Controllers;

// Implementa o lado "SOFTWARE SERVICE" de POST /v1/newEvent — alternativa push ao polling
// (GET /v1/events:polling) já coberto por KeetaEventPollingBackgroundService. Ambos os caminhos
// convergem no mesmo IKeetaOrderEventProcessor, então habilitar/desabilitar um dos dois modos do
// lado da Keeta não duplica nem perde eventos (idempotência por eventId).
[ApiController]
[Route("api/webhook/keeta/order-events")]
public sealed class KeetaOrderEventWebhookController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        string rawPayload;
        using (var reader = new StreamReader(Request.Body))
        {
            rawPayload = await reader.ReadToEndAsync(cancellationToken);
        }

        var appId = Request.Headers["X-App-Id"].FirstOrDefault();
        var merchantIdHeader = Request.Headers["X-App-MerchantId"].FirstOrDefault();
        var signature = Request.Headers["X-App-Signature"].FirstOrDefault();
        long? merchantId = long.TryParse(merchantIdHeader, out var parsedMerchantId) ? parsedMerchantId : null;

        var result = await mediator.Send(new ProcessKeetaNewEventWebhookCommand(rawPayload, appId, merchantId, signature), cancellationToken);

        if (result.IsFailure && result.Error.Code is "Keeta.InvalidSignature" or "Keeta.MissingSignature")
            return Unauthorized();

        if (result.IsFailure && result.Error.Code is "Keeta.InvalidPayload" or "Keeta.UnknownMerchant")
            return BadRequest();

        // Status 200 disponível por compatibilidade — a doc recomenda 204.
        return NoContent();
    }
}
