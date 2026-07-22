using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Suppliers.Create;
using SyncBar.Application.Features.Suppliers.Deactivate;
using SyncBar.Application.Features.Suppliers.GetByCompany;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Estoque")]
public sealed class SuppliersController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("company/{companyId:long}")]
    public async Task<IActionResult> GetByCompany(long companyId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetSuppliersByCompanyQuery(companyId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupplierCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPut("{id:long}/deactivate")]
    public async Task<IActionResult> Deactivate(long id, CancellationToken ct)
    {
        var result = await Mediator.Send(new DeactivateSupplierCommand(id), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
}
