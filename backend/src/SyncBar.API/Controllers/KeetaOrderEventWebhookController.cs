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
    public async Task<IActionResult> Receive(
        [FromHeader(Name = "X-App-Id")] string? appId,
        [FromHeader(Name = "X-App-MerchantId")] string? merchantIdHeader,
        [FromHeader(Name = "X-App-Signature")] string? signature,
        CancellationToken cancellationToken)
    {
        // Corpo lido como texto bruto de propósito — mesma justificativa do
        // KeetaAuthorizationWebhookController: a assinatura HMAC é calculada sobre os bytes
        // exatos do payload e a validação de JSON é feita no handler (Keeta.InvalidPayload) com
        // contrato de erro próprio, não pelo model binder do framework.
        string rawPayload;
        using (var reader = new StreamReader(Request.Body))
        {
            rawPayload = await reader.ReadToEndAsync(cancellationToken);
        }

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
