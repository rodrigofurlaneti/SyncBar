using MediatR;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Integrations.Keeta.Authorization.ProcessAuthorizationWebhook;

namespace SyncBar.API.Controllers;

[ApiController]
[Route("api/webhook/keeta/authorization")]
public sealed class KeetaAuthorizationWebhookController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        string rawPayload;
        using (var reader = new StreamReader(Request.Body))
        {
            rawPayload = await reader.ReadToEndAsync(cancellationToken);
        }

        var signature = Request.Headers["X-App-Signature"].FirstOrDefault();

        var result = await mediator.Send(new ProcessKeetaAuthorizationWebhookCommand(rawPayload, signature), cancellationToken);

        if (result.IsFailure && result.Error.Code is "Keeta.InvalidSignature" or "Keeta.MissingSignature")
            return Unauthorized();

        if (result.IsFailure && result.Error.Code == "Keeta.InvalidPayload")
            return BadRequest();

        return Ok();
    }
}
