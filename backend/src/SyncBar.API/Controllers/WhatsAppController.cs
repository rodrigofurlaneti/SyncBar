using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Integrations.WhatsApp.SendImage;

namespace SyncBar.API.Controllers;

[Authorize(Roles = ManagerRoles)]
public sealed class WhatsAppController(IMediator mediator) : ApiController(mediator)
{
    [HttpPost("images")]
    public async Task<IActionResult> SendImage(SendWhatsAppImageCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : Accepted(new { message = "Envio agendado." });
    }
}
