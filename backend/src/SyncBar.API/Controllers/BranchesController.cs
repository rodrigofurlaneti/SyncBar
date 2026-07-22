using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Branches.Create;
using SyncBar.Application.Features.Branches.GetByCompany;
using SyncBar.Application.Features.Branches.SetSelfServiceEmployee;

namespace SyncBar.API.Controllers;

// Gestão de filiais é prerrogativa do administrador da empresa (multi-loja).
[Authorize(Roles = "Administrador")]
public sealed class BranchesController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("company/{companyId:long}")]
    public async Task<IActionResult> GetByCompany(long companyId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetBranchesByCompanyQuery(companyId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBranchCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    // Configura qual funcionário "abre" os pedidos lançados pelo autoatendimento via QR Code.
    [HttpPut("self-service-employee")]
    public async Task<IActionResult> SetSelfServiceEmployee([FromBody] SetSelfServiceEmployeeCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
}
