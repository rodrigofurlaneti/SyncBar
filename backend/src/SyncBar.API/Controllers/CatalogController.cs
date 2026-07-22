using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Catalog.GetMenu;
using SyncBar.Application.Features.Promotions.GetActive;

namespace SyncBar.API.Controllers;

[Authorize]
public sealed class CatalogController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("promotions/active/branch/{branchId:long}")]
    public async Task<IActionResult> GetActivePromotions(long branchId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetActivePromotionsQuery(branchId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpGet("menu/company/{companyId:long}")]
    public async Task<IActionResult> GetMenu(long companyId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMenuQuery(companyId), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }
}
