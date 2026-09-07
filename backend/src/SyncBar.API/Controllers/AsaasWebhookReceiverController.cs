using MediatR;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Receive;

namespace SyncBar.API.Controllers;

[ApiController]
[Route("api/webhook/asaas")]
public sealed class AsaasWebhookReceiverController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(
        [FromBody] System.Text.Json.JsonElement payload,
        [FromHeader(Name = "asaas-access-token")] string? accessToken,
        CancellationToken cancellationToken)
    {
        var rawPayload = payload.GetRawText();

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await mediator.Send(
            new ReceiveAsaasWebhookCommand(rawPayload, accessToken, ipAddress),
            cancellationToken);

        if (result.IsFailure && result.Error.Code == "Asaas.InvalidWebhookToken")
            return Unauthorized();

        return Ok();
    }
}
