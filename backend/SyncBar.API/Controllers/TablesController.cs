using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Tables.GenerateQrToken;
using SyncBar.Application.Features.Tables.GetByBranch;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Salao")]
public sealed class TablesController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("branch/{branchId:long}")]
    public async Task<IActionResult> GetByBranch(long branchId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetTablesByBranchQuery(branchId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    // Gera (ou regenera) o link/QR Code de autoatendimento da mesa — gerente/admin.
    [Authorize(Roles = "Administrador,Gerente")]
    [HttpPost("{id:long}/qr-token")]
    public async Task<IActionResult> GenerateQrToken(long id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GenerateTableQrTokenCommand(id), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(new { token = result.Value });
    }
}
