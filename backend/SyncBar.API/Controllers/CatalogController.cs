using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Catalog.GetMenu;

namespace SyncBar.API.Controllers;

[Authorize]
public sealed class CatalogController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("menu/company/{companyId:long}")]
    public async Task<IActionResult> GetMenu(long companyId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMenuQuery(companyId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }
}
