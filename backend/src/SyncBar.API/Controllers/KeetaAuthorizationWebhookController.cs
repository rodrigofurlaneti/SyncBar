using MediatR;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Integrations.Keeta.Authorization.ProcessAuthorizationWebhook;

namespace SyncBar.API.Controllers;

[ApiController]
[Route("api/webhook/keeta/authorization")]
public sealed class KeetaAuthorizationWebhookController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(
        [FromHeader(Name = "X-App-Signature")] string? signature,
        CancellationToken cancellationToken)
    {
        // O corpo é lido como texto bruto (em vez de [FromBody] em um DTO) de propósito: a
        // assinatura HMAC (X-App-Signature) é calculada pela Keeta sobre os bytes exatos do
        // payload, e a validação de "é um JSON válido?" é feita no handler (Keeta.InvalidPayload)
        // — não pelo model binder — para devolver um contrato de erro consistente em vez do
        // ProblemDetails genérico do framework. Fazer o parse aqui via [FromBody] rejeitaria
        // payloads malformados antes da assinatura ser verificada e antes do log de auditoria.
        string rawPayload;
        using (var reader = new StreamReader(Request.Body))
        {
            rawPayload = await reader.ReadToEndAsync(cancellationToken);
        }

        var result = await mediator.Send(new ProcessKeetaAuthorizationWebhookCommand(rawPayload, signature), cancellationToken);

        if (result.IsFailure && result.Error.Code is "Keeta.InvalidSignature" or "Keeta.MissingSignature")
            return Unauthorized();

        if (result.IsFailure && result.Error.Code == "Keeta.InvalidPayload")
            return BadRequest();

        return Ok();
    }
}
