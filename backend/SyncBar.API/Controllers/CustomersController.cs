using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncBar.Application.Features.Customers.AddLoyaltyPoints;
using SyncBar.Application.Features.Customers.Create;
using SyncBar.Application.Features.Customers.GetByCompany;

namespace SyncBar.API.Controllers;

[Authorize(Policy = "Feature:Salao")]
public sealed class CustomersController(IMediator mediator) : ApiController(mediator)
{
    [HttpGet("company/{companyId:long}")]
    public async Task<IActionResult> GetByCompany(long companyId, [FromQuery] string? search, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCustomersByCompanyQuery(companyId, search), ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.IsFailure ? HandleFailure(result) : Ok(result.Value);
    }

    [HttpPut("{id:long}/loyalty-points")]
    public async Task<IActionResult> AddLoyaltyPoints(long id, [FromBody] AddLoyaltyPointsRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new AddLoyaltyPointsCommand(id, request.Points), ct);
        return result.IsFailure ? HandleFailure(result) : NoContent();
    }
}

public sealed record AddLoyaltyPointsRequest(int Points);
