using MediatR;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Integrations.Asaas.WebhookLog.Receive;

namespace SyncBar.API.Controllers;

[ApiController]
[Route("api/webhook/asaas")]
public sealed class AsaasWebhookReceiverController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        string rawPayload;
        using (var reader = new StreamReader(Request.Body))
        {
            rawPayload = await reader.ReadToEndAsync(cancellationToken);
        }

        var accessToken = Request.Headers["asaas-access-token"].FirstOrDefault();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await mediator.Send(
            new ReceiveAsaasWebhookCommand(rawPayload, accessToken, ipAddress),
            cancellationToken);

        if (result.IsFailure && result.Error.Code == "Asaas.InvalidWebhookToken")
            return Unauthorized();

        return Ok();
    }
}
