using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Comandas.GetByBranch;
using SyncBar.Application.Features.Comandas.Settings;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Salao")]
public sealed class ComandasController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("branch/{branchId:long}")]
    public async Task<IActionResult> GetByBranch(long branchId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetComandasByBranchQuery(branchId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpGet("settings/branch/{branchId:long}")]
    public async Task<IActionResult> GetSettings(long branchId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetComandaSettingQuery(branchId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    // Limite padrao: so o gerente altera.
    [Authorize(Roles = "Administrador,Gerente")]
    [HttpPut("settings")]
    public async Task<IActionResult> SetDefaultLimit([FromBody] SetComandaDefaultLimitCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
}
